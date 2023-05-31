using System;
using ExtendedVariants.Module;

namespace TAS.Utils;

internal static class ExtendedVariantsUtils {
    private static readonly Lazy<bool> Installed = new(() => ModUtils.GetModule("ExtendedVariantMode") != null);

    // enum value might be different between different ExtendedVariantMode version
    private static readonly Lazy<object> UpsideDownVariant =
        new(() => Enum.Parse(typeof(ExtendedVariantsModule.Variant), "UpsideDown"));

    private static readonly Lazy<object> SuperDashingVariant =
        new(() => Enum.Parse(typeof(ExtendedVariantsModule.Variant), "SuperDashing"));

    public static bool UpsideDown =>
        Installed.Value &&
        (bool)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue((ExtendedVariantsModule.Variant)UpsideDownVariant.Value);

    public static bool SuperDashing =>
        Installed.Value &&
        (bool)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue((ExtendedVariantsModule.Variant)SuperDashingVariant.Value);
}