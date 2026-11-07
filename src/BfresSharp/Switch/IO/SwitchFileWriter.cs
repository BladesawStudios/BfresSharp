using BfresSharp.Model;
using BfresSharp.Switch.Animation;
using BfresSharp.Switch.Relocation;

namespace BfresSharp.Switch.IO;

/// <summary>Writes a <see cref="ResFile"/> as a Switch BFRES file.</summary>
/// <remarks>
/// Lays the file out in the order the game's tools do: the header, the arrays of section headers, the dictionaries,
/// what each section points to, the name pool, and the relocation table last. Pointer fields are recorded as they are
/// written, so the relocation table and every size and offset in the header come from the layout itself.
/// </remarks>
public static class SwitchFileWriter
{
    private const int BufferInfoSize = 0x20;
    private const int FlagOffset = 0x14;
    private const int BlockOffsetField = 0x16;

    public static byte[] Write(ResFile file)
    {
        var writer = new SwitchWriter(SwitchProfile.For(file.Version));
        RootSlots root = WriteHeader(writer, file);

        if (file.SkeletalAnims.Count > 0) writer.Resolve(root.SkeletalAnims);
        var skeletal = file.SkeletalAnims.Values.Select(anim => SkeletalAnimCodec.WriteHeader(writer, anim)).ToList();
        WriteBufferInfoPlaceholder(writer, file);
        WriteDictionary(writer, root.SkeletalAnimDict, file.SkeletalAnims);
        for (int index = 0; index < skeletal.Count; index++) SkeletalAnimCodec.WriteBody(writer, file.SkeletalAnims.Values[index], skeletal[index]);

        int poolStart = WriteStringPool(writer, file, root);
        return Finish(writer, file, root, poolStart);
    }

    private static RootSlots WriteHeader(SwitchWriter writer, ResFile file)
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
        writer.Pointer();
        writer.Pointer();
        if (writer.Profile.Modern)
        {
            for (int word = 0; word < 4; word++) writer.Unlisted(BitConverter.ToUInt64(file.Reserved, word * 8));
        }

        int skeletalArray = writer.Pointer();
        int skeletalDict = writer.Pointer();
        for (int slot = 0; slot < 9; slot++) writer.Pointer();
        writer.Unlisted();
        writer.Pointer();
        writer.Pointer();
        writer.Unlisted();
        int stringTable = writer.Pointer();
        int poolSize = writer.Position;
        o.U32(0).U16(0);
        if (writer.Profile.Modern) o.U16(0).U16(0);
        o.U16((ushort)file.SkeletalAnims.Count).U16(0).U16(0).U16(0).U16(0).U16(0);
        if (writer.Profile.Modern) o.U8(file.ExternalFlags).U8(file.SavedFlags);
        else o.Zeros(6);
        return new RootSlots(nameOffset, tableOffset, fileSize, skeletalArray, skeletalDict, stringTable, poolSize);
    }

    private static void WriteDictionary<T>(SwitchWriter writer, int slot, ResDict<T> dict)
    {
        if (dict.Count == 0) return;
        writer.Align(8).Resolve(slot);
        SwitchDictCodec.Write(writer, dict);
    }

    // A file without buffers still carries an empty buffer info block; the header does not point at it.
    private static void WriteBufferInfoPlaceholder(SwitchWriter writer, ResFile file)
    {
        if (writer.Profile.Modern) return;
        writer.Out.U32(0x22).Zeros(BufferInfoSize - 4);
    }

    private static int WriteStringPool(SwitchWriter writer, ResFile file, RootSlots root)
    {
        writer.Align(4);
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

    private static byte[] Finish(SwitchWriter writer, ResFile file, RootSlots root, int poolStart)
    {
        int stringEnd = writer.Position;
        writer.Align(8);
        int tableAt = writer.Position;
        var sections = new List<RelocationSection>
        {
            new(0, (uint)stringEnd, [.. RelocationEncoder.Encode(writer.Pointers.Order().ToList())]),
        };
        for (int unused = 0; unused < 4; unused++) sections.Add(new RelocationSection((uint)stringEnd, 0, []));
        writer.Out.Bytes(new RelocationTable(sections).ToBytes((uint)tableAt));

        PatchBlocks(writer, file, tableAt);
        writer.Out.SetU32(root.TableOffsetAt, (uint)tableAt);
        writer.Out.SetU32(root.FileSizeAt, (uint)writer.Position);
        return writer.Out.ToArray();
    }

    // Chains the linked header blocks: each holds the distance to the next, the last the distance to the relocation table.
    private static void PatchBlocks(SwitchWriter writer, ResFile file, int tableAt)
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

        _ = file;
    }

    private sealed record RootSlots(
        int NameOffsetAt,
        int TableOffsetAt,
        int FileSizeAt,
        int SkeletalAnims,
        int SkeletalAnimDict,
        int StringTable,
        int PoolSizeAt);
}
