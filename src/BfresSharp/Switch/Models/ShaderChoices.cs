using BfresSharp.Model.Material;

namespace BfresSharp.Switch.Models;

/// <summary>The names a material chooses for its attribute or sampler assignments and which key takes which.</summary>
/// <remarks>
/// Distinct chosen names are stored once, in the order of first use unless the layout read from the file still fits. A key left at the shader's default takes no name and slot -1. The slot array is only
/// stored when some key takes a slot other than its own position.
/// </remarks>
public sealed class Choices
{
    public List<string> Names { get; } = [];

    public List<int> Slots { get; } = [];

    public bool NeedsIndices => Slots.Select((slot, index) => slot != index).Any(different => different);

    // For each name, the position of the first key that takes it.
    public IEnumerable<int> KeysByName() => Names.Select((_, name) => Slots.IndexOf(name));

    public static Choices Of(IEnumerable<string> values, ChoiceLayout? layout = null)
    {
        var choices = new Choices();
        var chosen = values.ToList();
        if (layout is not null && layout.Matches(chosen))
        {
            choices.Names.AddRange(layout.Names);
            choices.Slots.AddRange(layout.Slots);
            return choices;
        }

        foreach (string value in chosen)
        {
            if (value == ShaderAssign.DefaultValue)
            {
                choices.Slots.Add(-1);
                continue;
            }

            int slot = choices.Names.IndexOf(value);
            if (slot < 0) choices.Names.Add(value);
            choices.Slots.Add(slot < 0 ? choices.Names.Count - 1 : slot);
        }

        return choices;
    }
}

/// <summary>The choices a material makes for shader options: switches kept as bits, other values kept as names.</summary>
/// <remarks>
/// A slot counts the switches first and the named values after them. The slot array is stored when some option is left
/// at its default or the options are not already in that order.
/// </remarks>
public sealed class OptionChoices
{
    public List<bool> Switches { get; } = [];

    public List<string> Values { get; } = [];

    public List<int> Slots { get; } = [];

    public bool NeedsIndices { get; private set; }

    // For each name, the position of the first key that takes it.
    public IEnumerable<int> KeysByName() => Enumerable.Range(0, Switches.Count + Values.Count).Select(name => Slots.IndexOf(name));

    public static OptionChoices Of(IEnumerable<string> values, ChoiceLayout? layout = null)
    {
        var choices = new OptionChoices();
        var entries = values.ToList();
        if (layout is not null && layout.Matches(entries))
        {
            choices.Switches.AddRange(layout.Names.Take(layout.Switches).Select(name => name == "True"));
            choices.Values.AddRange(layout.Names.Skip(layout.Switches));
            choices.Slots.AddRange(layout.Slots);
            choices.NeedsIndices = choices.Slots.Select((slot, index) => slot != index).Any(different => different);
            return choices;
        }

        foreach (string value in entries)
        {
            if (value == "True" || value == "False") choices.Switches.Add(value == "True");
            else if (value != ShaderAssign.DefaultValue) choices.Values.Add(value);
        }

        int switches = 0, named = 0;
        foreach (string value in entries)
        {
            if (value == ShaderAssign.DefaultValue) choices.Slots.Add(-1);
            else if (value == "True" || value == "False") choices.Slots.Add(switches++);
            else choices.Slots.Add(choices.Switches.Count + named++);
        }

        choices.NeedsIndices = choices.Slots.Select((slot, index) => slot != index).Any(different => different);
        return choices;
    }

    public ulong Word(int index)
    {
        ulong word = 0;
        for (int bit = index * 64; bit < Math.Min(Switches.Count, index * 64 + 64); bit++)
        {
            if (Switches[bit]) word |= 1UL << (bit - index * 64);
        }

        return word;
    }
}
