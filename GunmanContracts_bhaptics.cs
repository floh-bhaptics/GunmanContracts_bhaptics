using HarmonyLib;
using Il2Cpp;
using Il2CppHurricaneVR.Framework.Core;
using Il2CppHurricaneVR.Framework.Core.Bags;
using Il2CppHurricaneVR.Framework.Core.Grabbers;
using Il2CppHurricaneVR.Framework.Core.Player;
using Il2CppHurricaneVR.Framework.Weapons;
using Il2CppHurricaneVR.Framework.Weapons.Bow;
using Il2CppHurricaneVR.Framework.Weapons.Guns;
using Il2CppKnifePlayerController;
using MelonLoader;
using MyBhapticsTactsuit;
using System;
using UnityEngine;

[assembly: MelonInfo(typeof(GunmanContracts_bhaptics.GunmanContracts_bhaptics), "GunmanContracts_bhaptics", "1.0.2", "Florian Fahrenberger")]
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

        private static (float angle, float shift) GetHapticsDirection(Transform player, Vector3 hitPosition)
        {
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            Vector3 relativeHit = hitPosition - player.position;
            Vector3 playerDir = player.rotation.eulerAngles;

            Vector3 flattenedHit = new Vector3(relativeHit.x, 0f, relativeHit.z);
            float hitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            Vector3 crossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (crossProduct.y > 0f) hitAngle *= -1f;

            float myRotation = hitAngle - playerDir.y;
            myRotation *= -1f;
            if (myRotation < 0f) myRotation = 360f + myRotation;

            float hitShift = relativeHit.y;
            float upperBound = 0.0f;
            float lowerBound = -0.5f;
            if (hitShift > upperBound) hitShift = 0.5f;
            else if (hitShift < lowerBound) hitShift = -0.5f;
            else hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f;

            return (myRotation, hitShift);
        }

        [HarmonyPatch(typeof(ANBGameLogic), "HurtPlayer")]
        public class bhaptics_HurtPlayer
        {
            [HarmonyPostfix]
            public static void Postfix(ANBGameLogic __instance, string type, float dmg, ANBBasicNPC attacker)
            {
                if (attacker == null) return;

                var (angle, shift) = GetHapticsDirection(Camera.main.transform, attacker.transform.position);
                tactsuitVr.PlayBackHit("impact", angle, shift);
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "CheckForCheats")]
        public class bhaptics_DisableCheatCheck
        {
            [HarmonyPostfix]
            public static void Postfix(ANBGameLogic __instance, ref bool __result)
            {
                if ((__instance.CheatGod) ||
                    (__instance.CheatGunsDontKill) ||
                    (__instance.CheatInfititeLastHP) ||
                    (__instance.CheatInvisible) ||
                    (__instance.CheatSlowmotion) ||
                    (__instance.CheatUnlimitedMag)
                    ) __result = true;
                else __result = false;
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "holsterGun")]
        public class bhaptics_HolsterGun
        {
            [HarmonyPostfix]
            public static void Postfix(string side)
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
            public static void Postfix(string side)
            {
                bool isRight = ((side == "right") || (side == "backRight"));
                bool isBackHolster = ((side == "backLeft") || (side == "backRight"));
                tactsuitVr.PlayHolsterOut(isRight, isBackHolster);
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "holsterKnife")]
        public class bhaptics_HolsterKnife
        {
            [HarmonyPostfix]
            public static void Postfix(string side)
            {
                bool isRight = ((side == "right") || (side == "backRight"));
                bool isBackHolster = ((side == "backLeft") || (side == "backRight"));
                tactsuitVr.PlayHolsterIn(isRight, isBackHolster);
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "unholsterKnife")]
        public class bhaptics_UnholsterKnife
        {
            [HarmonyPostfix]
            public static void Postfix(string side)
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

        [HarmonyPatch(typeof(ANBAmmoBag), "removeAmmoCount")]
        public class bhaptics_AmmoPouchRemove
        {
            [HarmonyPostfix]
            public static void Postfix(string ammo, int removeVal)
            {
                tactsuitVr.PlaybackHaptics("ammo_pouch");
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "collectibleCollected")]
        public class bhaptics_Collectible
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ammo_pouch");
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "collectCoinWallet")]
        public class bhaptics_CollectCoins
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ammo_pouch");
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "creditAmmo")]
        public class bhaptics_getAmmo
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ammo_pouch");
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "substractAmmo")]
        public class bhaptics_removeAmmo
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ammo_pouch");
            }
        }

        [HarmonyPatch(typeof(ANBSFXPlayerManager), "TakeDamageLastHP")]
        public class bhaptics_StartHeartbeat
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StartHeartBeat();
            }
        }

        [HarmonyPatch(typeof(ANBSFXPlayerManager), "Heal")]
        public class bhaptics_StopHeartbeat
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopHeartBeat();
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "PlayerDeadCall")]
        public class bhaptics_StopHeartbeatOnDeath
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopHeartBeat();
            }
        }

        [HarmonyPatch(typeof(ANBGameLogic), "endRun")]
        public class bhaptics_StopHeartbeatOnEndRun
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopHeartBeat();
            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "ReleaseAmmo")]
        public class bhaptics_ReleaseAmmo
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance)
            {
                if (__instance.gunInHand == "Right") tactsuitVr.PlaybackHaptics("Eject_Mag_R");
                else tactsuitVr.PlaybackHaptics("Eject_Mag_L");

            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "OnAmmoSocketed")]
        public class bhaptics_AmmoSocketed
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance)
            {
                if (__instance.gunInHand == "none") return;
                if (__instance.gunInHand == "Right") tactsuitVr.PlaybackHaptics("Reload_R");
                else tactsuitVr.PlaybackHaptics("Reload_L");

            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "OnHandGrabbed")]
        public class bhaptics_HandGrabbed
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance)
            {
                if (__instance.gunInHand == "Right") tactsuitVr.PlaybackHaptics("Eject_Mag_R");
                else tactsuitVr.PlaybackHaptics("Eject_Mag_L");

            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "AddShotgunShell")]
        public class bhaptics_AddShell
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance)
            {
                if (__instance.gunInHand == "Right") tactsuitVr.PlaybackHaptics("Reload_R");
                else tactsuitVr.PlaybackHaptics("Reload_L");

            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "OnCockingHandleEjected")]
        public class bhaptics_CockEjected
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance)
            {
                if (__instance.gunInHand == "Right") tactsuitVr.PlaybackHaptics("cock_slideback_r");
                else tactsuitVr.PlaybackHaptics("cock_slideback_l");

            }
        }

        [HarmonyPatch(typeof(ANBHVRGunBase), "OnCockingHandleReleased")]
        public class bhaptics_CockReleased
        {
            [HarmonyPostfix]
            public static void Postfix(ANBHVRGunBase __instance)
            {
                if (__instance.gunInHand == "Right") tactsuitVr.PlaybackHaptics("cock_slidefront_r");
                else tactsuitVr.PlaybackHaptics("cock_slidefront_l");

            }
        }

    }
}
