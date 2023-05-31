using System;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.Module;
using TAS.Utils;

namespace TAS.EverestInterop.Hitboxes;

public static class HitboxConquerorBeam {
    private static GetDelegate<Entity, float> getChargeTimer;
    private static GetDelegate<Entity, float> getActiveTimer;
    private static GetDelegate<Entity, float> getAngle;
    private static GetDelegate<Entity, Entity> getBoss;
    private static Type conquerorBeamType;

    [Initialize]
    private static void Initialize() {
        conquerorBeamType = ModUtils.GetType("Conqueror's Peak", "Celeste.Mod.ricky06ModPack.Entities.ConquerorBeam");

        if (conquerorBeamType != null) {
            getChargeTimer = conquerorBeamType.CreateGetDelegate<Entity, float>("chargeTimer");
            getActiveTimer = conquerorBeamType.CreateGetDelegate<Entity, float>("activeTimer");
            getAngle = conquerorBeamType.CreateGetDelegate<Entity, float>("angle");
            getBoss = conquerorBeamType.CreateGetDelegate<Entity, Entity>("boss");
            On.Monocle.Entity.DebugRender += ModHitbox;
        }
    }

    [Unload]
    private static void Unload() {
        On.Monocle.Entity.DebugRender -= ModHitbox;
    }

    private static void ModHitbox(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (!TasSettings.ShowHitboxes) {
            orig(self, camera);
            return;
        }

        orig(self, camera);

        if (self.GetType() == conquerorBeamType && getChargeTimer(self) <= 0f && getActiveTimer(self) > 0f) {
            float angle = getAngle(self);
            Entity boss = getBoss(self);
            Vector2 vector = boss.Center + Calc.AngleToVector(angle, 12f);
            Vector2 vector2 = boss.Center + Calc.AngleToVector(angle, 2000f);
            Vector2 value = (vector2 - vector).Perpendicular().SafeNormalize(2f);
            Draw.Line(vector + value, vector2 + value, HitboxColor.EntityColor);
            Draw.Line(vector - value, vector2 - value, HitboxColor.EntityColor);
            Draw.Line(vector, vector2, HitboxColor.EntityColor);
        }
    }
}