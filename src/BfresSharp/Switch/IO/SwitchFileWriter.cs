using BfresSharp.Container;
using BfresSharp.Model;
using BfresSharp.Switch.Animation;
using BfresSharp.Switch.Common;
using BfresSharp.Switch.Models;
using BfresSharp.Switch.Relocation;

namespace BfresSharp.Switch.IO;

/// <summary>Writes a <see cref="ResFile"/> as a Switch BFRES file.</summary>
/// <remarks>
/// Lays the file out in the order the game's tools do: the header, the arrays of section headers, the dictionaries,
/// what each section points to, the name pool, the buffer data, the memory pool and the relocation table last. Pointer
/// fields are recorded as they are written, so the relocation table and every size and offset in the header come from
/// the layout itself.
/// </remarks>
public static class SwitchFileWriter
{
    private const int BufferInfoSize = 0x20;
    private const int BlockOffsetField = 0x16;
    private const int RelocationRoom = 1 << 16;

    public static byte[] Write(ResFile file) => Build(file).ToArray();

    // Writes straight from the builder, which saves copying a finished file into an array first.
    public static void Write(ResFile file, Stream destination) => destination.Write(Build(file).Written);

    private static Binary.ByteBuilder Build(ResFile file)
    {
        var writer = new SwitchWriter(SwitchProfile.For(file.Version));
        var layout = new SwitchBufferLayout(file);
        RootSlots root = WriteHeader(writer, file, layout);

        if (file.Models.Count > 0) writer.Resolve(root.Models);
        var models = file.Models.Values.Select(model => ModelCodec.WriteHeader(writer, model)).ToList();
        if (file.SkeletalAnims.Count > 0) writer.Resolve(root.SkeletalAnims);
        var skeletal = file.SkeletalAnims.Values.Select(anim => SkeletalAnimCodec.WriteHeader(writer, anim)).ToList();
        if (file.MaterialAnims.Count > 0) writer.Resolve(root.MaterialAnims);
        var material = file.MaterialAnims.Values.Select(anim => MaterialAnimCodec.WriteHeader(writer, anim)).ToList();
        if (file.VisibilityAnims.Count > 0) writer.Resolve(root.VisibilityAnims);
        var visibility = file.VisibilityAnims.Values.Select(anim => VisibilityAnimCodec.WriteHeader(writer, anim)).ToList();
        if (file.ShapeAnims.Count > 0) writer.Resolve(root.ShapeAnims);
        var shapes = file.ShapeAnims.Values.Select(anim => ShapeAnimCodec.WriteHeader(writer, anim)).ToList();
        int bufferData = WriteBufferInfo(writer, root, layout, file, models);
        if (file.ExternalFiles.Count > 0) writer.Align(8).Resolve(root.ExternalFiles);
        int[] externalSlots = ExternalFileCodec.WriteEntries(writer, file.ExternalFiles);
        WriteDictionary(writer, root.ModelDict, file.Models);
        WriteDictionary(writer, root.SkeletalAnimDict, file.SkeletalAnims);
        WriteDictionary(writer, root.MaterialAnimDict, file.MaterialAnims);
        WriteDictionary(writer, root.VisibilityAnimDict, file.VisibilityAnims);
        WriteDictionary(writer, root.ShapeAnimDict, file.ShapeAnims);
        WriteDictionary(writer, root.ExternalFileDict, file.ExternalFiles);
        for (int index = 0; index < models.Count; index++) ModelCodec.WriteBody(writer, file.Models.Values[index], models[index], layout);
        for (int index = 0; index < skeletal.Count; index++) SkeletalAnimCodec.WriteBody(writer, file.SkeletalAnims.Values[index], skeletal[index]);
        for (int index = 0; index < visibility.Count; index++) VisibilityAnimCodec.WriteBody(writer, file.VisibilityAnims.Values[index], visibility[index]);
        for (int index = 0; index < shapes.Count; index++) ShapeAnimCodec.WriteBody(writer, file.ShapeAnims.Values[index], shapes[index]);
        for (int index = 0; index < material.Count; index++) MaterialAnimCodec.WriteBody(writer, file.MaterialAnims.Values[index], material[index]);

        int poolStart = WriteStringPool(writer, file, root);
        return Finish(writer, file, root, layout, bufferData, externalSlots);
    }

    private static RootSlots WriteHeader(SwitchWriter writer, ResFile file, SwitchBufferLayout layout)
    {
        var o = writer.Out;
        o.Bytes("FRES    "u8).U32(file.Version.Raw).U16(0xFEFF).U8(file.Alignment).U8(file.TargetAddressSize);
        int nameOffset = writer.Position;
        o.U32(0).U16(file.Flag).U16(0);
        int tableOffset = writer.Position;
        o.U32(0);
        int fileSize = writer.Position;
        o.U32(0);
        writer.StringPointer(file.Name);
        int modelArray = SectionArray(writer, file.Models.Count > 0);
        int modelDict = writer.Pointer();
        if (writer.Profile.Modern)
        {
            for (int word = 0; word < 4; word++) writer.Unlisted(BitConverter.ToUInt64(file.Reserved, word * 8));
        }

        int skeletalArray = SectionArray(writer, file.SkeletalAnims.Count > 0);
        int skeletalDict = writer.Pointer();
        int materialArray = SectionArray(writer, file.MaterialAnims.Count > 0);
        int materialDict = writer.Pointer();
        int visibilityArray = SectionArray(writer, file.VisibilityAnims.Count > 0);
        int visibilityDict = writer.Pointer();
        int shapeArray = SectionArray(writer, file.ShapeAnims.Count > 0);
        int shapeDict = writer.Pointer();
        SectionArray(writer, present: false);
        writer.Pointer();

        if (!layout.IsEmpty) writer.PoolPointer();
        else if (writer.Profile.Modern && file.Platform != BfresPlatform.Switch2) writer.Pointer();
        else writer.Unlisted();
        int bufferInfo = layout.IsEmpty ? writer.Unlisted() : writer.Pointer();
        int externalArray = SectionArray(writer, file.ExternalFiles.Count > 0);
        int externalDict = writer.Pointer();
        writer.Unlisted();
        int stringTable = writer.Pointer();
        int poolSize = writer.Position;
        o.U32(0).U16((ushort)file.Models.Count);
        if (writer.Profile.Modern) o.U16(0).U16(0);
        o.U16((ushort)file.SkeletalAnims.Count).U16((ushort)file.MaterialAnims.Count).U16((ushort)file.VisibilityAnims.Count).U16((ushort)file.ShapeAnims.Count).U16(0).U16((ushort)file.ExternalFiles.Count);
        if (writer.Profile.Modern) o.U8(file.ExternalFlags).U8(file.SavedFlags);
        else o.Zeros(6);
        return new RootSlots(nameOffset, tableOffset, fileSize, modelArray, modelDict, skeletalArray, skeletalDict, materialArray, materialDict, visibilityArray, visibilityDict, shapeArray, shapeDict, bufferInfo, externalArray, externalDict, stringTable, poolSize);
    }

    // Files before version 9 list an array pointer in the relocation table only when the array exists.
    private static int SectionArray(SwitchWriter writer, bool present)
    {
        return writer.Profile.Modern || present ? writer.Pointer() : writer.Unlisted();
    }

    private static void WriteDictionary<T>(SwitchWriter writer, int slot, ResDict<T> dict)
    {
        if (dict.Count == 0) return;
        writer.Align(8).Resolve(slot);
        SwitchDictCodec.Write(writer, dict);
    }

    // A file without buffers still carries an empty buffer info block; the header does not point at it.
    private static int WriteBufferInfo(SwitchWriter writer, RootSlots root, SwitchBufferLayout layout, ResFile file, List<ModelCodec.Slots> models)
    {
        if (!layout.IsEmpty && new ModelLayout(writer.Profile).HasExtension)
        {
            // Switch 2 files keep the buffer info in the last bytes of the first model's header.
            int at = models[0].ExtensionAt;
            writer.ResolveTo(root.BufferInfo, at);
            writer.Out.SetU32(at, file.BufferInfoKind);
            writer.Out.SetU32(at + 4, (uint)layout.TotalSize);
            writer.ListPointerAt(at + 8);
            return at + 8;
        }

        if (layout.IsEmpty)
        {
            if (!writer.Profile.Modern) writer.Out.U32(file.BufferInfoKind).Zeros(BufferInfoSize - 4);
            return 0;
        }

        writer.Resolve(root.BufferInfo);
        writer.Out.U32(file.BufferInfoKind).U32((uint)layout.TotalSize);
        int slot = writer.Pointer();
        writer.Out.Zeros(BufferInfoSize - 16);
        return slot;
    }

    private static int WriteStringPool(SwitchWriter writer, ResFile file, RootSlots root)
    {
        writer.Align(8);
        int start = writer.Position;
        writer.Out.Bytes("_STR"u8);
        writer.Block();
        writer.Out.U32((uint)writer.Strings.Keys.Count(text => text.Length > 0));
        writer.Resolve(root.StringTable);
        Dictionary<string, int> placed = writer.WriteStringPool(file.StringOrder);
        writer.Out.SetU32(root.PoolSizeAt, (uint)(writer.Position - start));
        writer.Out.SetU32(root.NameOffsetAt, (uint)placed[file.Name] + 2);
        return start;
    }

    private static Binary.ByteBuilder Finish(SwitchWriter writer, ResFile file, RootSlots root, SwitchBufferLayout layout, int bufferData, int[] externalSlots)
    {
        int stringEnd = writer.Position;
        writer.Out.Reserve(TailSize(file, layout));
        long bufferBase = 0, poolAt = 0;
        if (!layout.IsEmpty)
        {
            writer.Align(SwitchBufferLayout.PageSize);
            bufferBase = writer.Position;
            layout.BuildInto(writer.Out.Claim((int)layout.TotalSize));
            poolAt = writer.Position;
            writer.Out.Zeros(SwitchSectionBuilder.MemoryPoolSize);
            ResolveBuffers(writer, bufferData, bufferBase, poolAt);
        }

        long externalAt = file.ExternalFiles.Count == 0 ? 0 : ExternalFileCodec.WriteData(writer, file.ExternalFiles, externalSlots, SwitchBufferLayout.PageSize);
        writer.Align(ExternalFileCodec.TailAlignment(file.ExternalFiles, SwitchBufferLayout.PageSize));
        int tableAt = writer.Position;
        long externalEnd = tableAt;
        var sections = SwitchSectionBuilder.Build(writer, file, layout, new SectionBounds(stringEnd, bufferBase, poolAt, externalAt, externalEnd));
        writer.Out.Bytes(new RelocationTable(sections).ToBytes((uint)tableAt));

        PatchBlocks(writer, tableAt);
        writer.Out.SetU32(root.TableOffsetAt, (uint)tableAt);
        writer.Out.SetU32(root.FileSizeAt, (uint)writer.Position);
        return writer.Out;
    }

    // What the buffer data, the memory pool, the embedded files and the relocation table add, with room for their padding, so the output grows once.
    private static int TailSize(ResFile file, SwitchBufferLayout layout)
    {
        long external = file.ExternalFiles.Values.Sum(external => (long)external.Data.Length + SwitchBufferLayout.PageSize);
        return (int)Math.Min(int.MaxValue / 2, layout.TotalSize + SwitchSectionBuilder.MemoryPoolSize + SwitchBufferLayout.PageSize + external + RelocationRoom);
    }

    private static void ResolveBuffers(SwitchWriter writer, int bufferData, long bufferBase, long poolAt)
    {
        foreach (int slot in writer.PoolSlots) writer.ResolveTo(slot, poolAt);
        writer.ResolveTo(bufferData, bufferBase);
    }

    // Chains the linked header blocks: each holds the distance to the next, the last the distance to the relocation table.
    private static void PatchBlocks(SwitchWriter writer, int tableAt)
    {
        List<int> blocks = writer.BlockPositions;
        writer.Out.SetU16(BlockOffsetField, (ushort)(blocks[0] - 4));
        for (int index = 0; index < blocks.Count; index++)
        {
            bool last = index == blocks.Count - 1;
            uint distance = (uint)((last ? tableAt : blocks[index + 1] - 4) - (blocks[index] - 4));
            writer.Out.SetU32(blocks[index], last ? 0 : distance);
            writer.Out.SetU64(blocks[index] + 4, distance);
        }
    }

    private sealed record RootSlots(
        int NameOffsetAt,
        int TableOffsetAt,
        int FileSizeAt,
        int Models,
        int ModelDict,
        int SkeletalAnims,
        int SkeletalAnimDict,
        int MaterialAnims,
        int MaterialAnimDict,
        int VisibilityAnims,
        int VisibilityAnimDict,
        int ShapeAnims,
        int ShapeAnimDict,
        int BufferInfo,
        int ExternalFiles,
        int ExternalFileDict,
        int StringTable,
        int PoolSizeAt);
}
