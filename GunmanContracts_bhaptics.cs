using HarmonyLib;
using Il2Cpp;
using Il2CppHurricaneVR.Framework.Weapons.Bow;
using Il2CppHurricaneVR.Framework.Weapons.Guns;
using Il2CppKnifePlayerController;
using MelonLoader;
using MyBhapticsTactsuit;
using System;
using UnityEngine;

[assembly: MelonInfo(typeof(GunmanContracts_bhaptics.GunmanContracts_bhaptics), "GunmanContracts_bhaptics", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("ANB_Seth", "GunmanContracts")]

namespace GunmanContracts_bhaptics
{
    public class GunmanContracts_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;

        public override void OnInitializeMelon()
        {
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        private static (float angle, float shift) GetHapticsDirection(Vector3 hitDirection)
        {
            // bhaptics pattern: 0° = front, 90° = left, 270° = right, increasing clockwise.
            // Ignoring vertical component for now (shift stays 0).
            Vector3 flatDir = new Vector3(hitDirection.x, 0f, hitDirection.z);
            if (flatDir == Vector3.zero) return (0f, 0f);

            float angle = Vector3.SignedAngle(flatDir, Vector3.forward, Vector3.up);
            if (angle < 0f) angle += 360f;

            return (angle, 0f);
        }
        
        [HarmonyPatch(typeof(HealthManagerXtra), "TakeDamage")]
        public class bhaptics_TakeDamage
        {
            [HarmonyPostfix]
            public static void Postfix(HealthManagerXtra __instance, Vector3 direction)
            {
                tactsuitVr.LOG("Xtra!");
                try
                {
                    var (angle, shift) = GetHapticsDirection(direction);
                    tactsuitVr.PlayBackHit("bullet_hit", angle, shift);
                    if (__instance.dead) tactsuitVr.StopThreads();
                }
                catch (Exception ex)
                {
                    tactsuitVr.LOG($"bhaptics_Health_TakeDamage crashed: {ex}");
                }
            }
        }
        /*
        [HarmonyPatch(typeof(PlayerHealth), "TakeDamage")]
        public class bhaptics_Health_TakeDamage
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerHealth __instance, DamageData damage)
            {
                tactsuitVr.LOG("HealthDamage!");
                var (angle, shift) = GetHapticsDirection(damage.HitDirection);
                tactsuitVr.PlayBackHit("impact", angle, shift);
                if (__instance.health <= 0.25f * __instance.startHealth) tactsuitVr.StartHeartBeat();
                else tactsuitVr.StopHeartBeat();
                if (damage.Deadly) tactsuitVr.StopThreads();
                if (__instance.health <= 0.0f) tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(PlayerDamageHandler), "damaged")]
        public class bhaptics_PlayerDamaged
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerDamageHandler __instance, DamageData damage)
            {
                tactsuitVr.LOG("damaged!");
                try
                {
                    var (angle, shift) = GetHapticsDirection(damage.HitDirection);
                    tactsuitVr.PlayBackHit("bullet_hit", angle, shift);
                }
                catch (Exception ex)
                {
                    tactsuitVr.LOG($"bhaptics_Health_TakeDamage crashed: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerHealthBar), "Update")]
        public class bhaptics_checkHealth
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerHealthBar __instance)
            {
                if (__instance.health.HealthFraction <= 0.25f) tactsuitVr.StartHeartBeat();
                else tactsuitVr.StopHeartBeat();
                if (__instance.health.health <= 0.0f) tactsuitVr.StopThreads();
            }
        }
        
*/

        [HarmonyPatch(typeof(ANBGameLogic), "holsterGun")]
        public class bhaptics_HolsterGun
        {
            [HarmonyPostfix]
            public static void Postfix(string side, ANBHVRGunBase tmpGBS)
            {
                bool isRight = ((side == "right")||(side == "backRight"));
                bool isBackHolster = ((side == "backLeft") || (side == "backRight"));
                tactsuitVr.PlayHolsterIn(isRight, isBackHolster);
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "unholsterGun")]
        public class bhaptics_UnholsterGun
        {
            [HarmonyPostfix]
            public static void Postfix(string side, ANBHVRGunBase tmpGBS)
            {
                bool isRight = ((side == "right") || (side == "backRight"));
                bool isBackHolster = ((side == "backLeft") || (side == "backRight"));
                tactsuitVr.PlayHolsterOut(isRight, isBackHolster);
            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "OnFire")]
        public class bhaptics_GunFire
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance, Vector3 direction)
            {
                if (__instance.EnemyGun) return;
                if (__instance.isBow) return; // this class also drives the bow's flatscreen fallback — skip it here

                var primaryGrab = __instance.myGrabbable;
                if (primaryGrab == null) return; // shouldn't happen on fire, but just in case

                bool isRight = primaryGrab.IsRightHandGrabbed;
                bool isLeft = primaryGrab.IsLeftHandGrabbed;
                bool twoHanded = isRight && isLeft; // both hands somehow on the same grip point

                // Check whether the *other* hand is holding a second grip point (foregrip, rail, etc.)
                var hapticGrabbables = __instance.HapticGrabbables;
                if (!twoHanded && hapticGrabbables != null)
                {
                    for (int i = 0; i < hapticGrabbables.Count; i++)
                    {
                        var grabbable = hapticGrabbables[i];
                        if (grabbable == null || grabbable == primaryGrab) continue;

                        if ((isRight && grabbable.IsLeftHandGrabbed) || (isLeft && grabbable.IsRightHandGrabbed))
                        {
                            twoHanded = true;
                            break;
                        }
                    }
                }

                bool isShotgun = __instance.isShotgun;
                bool isRifle = (__instance.FireType == GunFireType.Automatic);

                tactsuitVr.GunRecoil(isRightHand: isRight, twoHanded: twoHanded, isShotgun: isShotgun, isRifle: isRifle);
            }
        }

        [HarmonyPatch(typeof(HVRPhysicsBow), "ShootArrow")]
        public class bhaptics_BowShoot
        {
            [HarmonyPostfix]
            public static void Postfix(HVRPhysicsBow __instance, Vector3 direction)
            {
                bool isRight = __instance.BowHand.IsRightHand;
                tactsuitVr.ShootBow(isRight);
            }
        }


    }
}
