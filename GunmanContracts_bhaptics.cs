using HarmonyLib;
using Il2Cpp;
using Il2CppHurricaneVR.Framework.Core.Player;
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
        /*
        private static bool heartbeatActive = false;

        [HarmonyPatch(typeof(ANBWristHud), "checkHealth")]
        public class bhaptics_CheckHealth
        {
            [HarmonyPostfix]
            public static void Postfix(ANBWristHud __instance)
            {
                tactsuitVr.LOG("checkHealth: " + __instance.healthColor.ToString() + " " + __instance.healthColorCritical.ToString());
                bool isCritical = __instance.healthColor == __instance.healthColorCritical;

                if (isCritical && !heartbeatActive)
                {
                    heartbeatActive = true;
                    tactsuitVr.StartHeartBeat();
                }
                else if (!isCritical && heartbeatActive)
                {
                    heartbeatActive = false;
                    tactsuitVr.StopHeartBeat();
                }
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
