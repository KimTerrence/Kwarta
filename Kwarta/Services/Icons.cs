namespace Kwarta.Services;

/// <summary>
/// Glyphs from the Material Icons Round font (Resources/Fonts/MaterialIconsRound-Regular.otf,
/// registered as "MaterialIcons"). Codepoints come from Google's material-design-icons repo.
/// </summary>
public static class Icons
{
    public const string FontFamily = "MaterialIcons";

    public const string Add = "\ue145";
    public const string ArrowDown = "\ue5db";
    public const string ArrowUp = "\ue5d8";
    public const string Bank = "\ue84f";
    public const string Calendar = "\ue935";
    public const string Cash = "\uef63";
    public const string PieChart = "\ue6c4";
    public const string Smartphone = "\ue32c";
    public const string Check = "\ue5ca";
    public const string ChevronLeft = "\ue5cb";
    public const string ChevronRight = "\ue5cc";
    public const string Dashboard = "\ue871";
    public const string Delete = "\ue872";
    public const string EditNote = "\ue745";
    public const string Receipt = "\uef6e";
    public const string Search = "\ue8b6";
    public const string SwapHoriz = "\ue8d4";
    public const string Wallet = "\ue850";
    public const string Warning = "\ue002";

    /// <summary>Icons a category can use, keyed by Material icon name (the value stored in the database).</summary>
    public static readonly IReadOnlyDictionary<string, string> Category = new Dictionary<string, string>
    {
        ["restaurant"] = "\ue56c",
        ["local_grocery_store"] = "\ue547",
        ["local_cafe"] = "\ue541",
        ["directions_bus"] = "\ue530",
        ["commute"] = "\ue940",
        ["local_gas_station"] = "\ue546",
        ["flight"] = "\ue539",
        ["home"] = "\ue88a",
        ["lightbulb"] = "\ue0f0",
        ["bolt"] = "\uea0b",
        ["water_drop"] = "\ue798",
        ["wifi"] = "\ue63e",
        ["phone_iphone"] = "\ue325",
        ["shopping_bag"] = "\uf1cc",
        ["checkroom"] = "\uf19e",
        ["medical_services"] = "\uf109",
        ["fitness_center"] = "\ueb43",
        ["spa"] = "\ueb4c",
        ["movie"] = "\ue02c",
        ["sports_esports"] = "\uea28",
        ["subscriptions"] = "\ue064",
        ["school"] = "\ue80c",
        ["child_care"] = "\ueb41",
        ["pets"] = "\ue91d",
        ["celebration"] = "\uea65",
        ["card_giftcard"] = "\ue8f6",
        ["volunteer_activism"] = "\uea70",
        ["build"] = "\ue869",
        ["category"] = "\ue574",
        ["work"] = "\ue8f9",
        ["storefront"] = "\uea12",
        ["payments"] = "\uef63",
        ["savings"] = "\ue2eb",
        ["trending_up"] = "\ue8e5",
        ["redeem"] = "\ue8b1",
    };

    public const string DefaultCategoryIcon = "category";

    public static string CategoryGlyph(string? key) =>
        key is not null && Category.TryGetValue(key, out var glyph) ? glyph : Category[DefaultCategoryIcon];

    public static string AccountGlyph(Models.AccountType type) => type switch
    {
        Models.AccountType.EWallet => Smartphone,
        Models.AccountType.Bank => Bank,
        _ => Cash
    };

    /// <summary>Colors a category can use. Saturated enough to read as an icon on a light tint of itself.</summary>
    public static readonly IReadOnlyList<string> CategoryColors =
    [
        "#F57C00", "#1E88E5", "#F9A825", "#8E24AA", "#D81B60", "#00897B",
        "#E53935", "#43A047", "#3949AB", "#6D4C41", "#00ACC1", "#607D8B",
    ];
}
