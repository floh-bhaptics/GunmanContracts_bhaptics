using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bhaptics.SDK2;
using MelonLoader;

namespace MyBhapticsTactsuit
{
    public class TactsuitVR
    {
        public bool suitDisabled = true;
        private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);


        public void HeartBeatFunc()
        {
            while (true)
            {
                HeartBeat_mrse.WaitOne();
                BhapticsSDK2.Play("HeartBeat".ToLower());
                Thread.Sleep(1000);
            }
        }

        public TactsuitVR()
        {
            LOG("Initializing suit");

            var res = BhapticsSDK2.Initialize("6aa2cd8e54f54ef8ac503eb0", "rFEVjPwquEdnVEeZ1djb");

            suitDisabled = res != 0;
            LOG("Starting HeartBeat and NeckTingle thread... " + res);
            Thread HeartBeatThread = new Thread(HeartBeatFunc);
            HeartBeatThread.Start();
        }

        public void LOG(string logStr)
        {
            MelonLogger.Msg(logStr);
        }


        public void PlaybackHaptics(String key, float intensity = 1.0f, float duration = 1.0f)
        {
            BhapticsSDK2.Play(key.ToLower(), intensity, duration, 0f, 0f);
        }

        public void PlayBackHit(String key, float xzAngle, float yShift)
        {
            // two parameters can be given to the pattern to move it on the vest:
            // 1. An angle in degrees [0, 360] to turn the pattern to the left
            // 2. A shift [-0.5, 0.5] in y-direction (up and down) to move it up or down
            BhapticsSDK2.Play(key.ToLower(), 1f, 1f, xzAngle, yShift);
        }

        public void GunRecoil(bool isRightHand, float intensity = 1.0f, bool twoHanded = false, bool isShotgun = false, bool isRifle = false)
        {
            float duration = 1.0f;
            string pattern = "recoil_";
            if (isShotgun) pattern += "shotgun";
            else if (isRifle) pattern += "rifle";
            else pattern += "pistol";
            if (twoHanded) pattern += "_twohanded";
            if (isRightHand) pattern += "_r";
            else pattern += "_l";

            BhapticsSDK2.Play(pattern.ToLower(), intensity, duration, 0f, 0f);

        }

        public void ShootBow(bool isRightHand, float intensity = 1.0f)
        {
            float duration = 1.0f;
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }

            string keyVest = "ShootBow" + postfix;

            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
        }


        public void HeadShot(float hitAngle)
        {
            if (BhapticsSDK2.IsDeviceConnected(PositionType.Head))
            {
                if ((hitAngle < 45f) | (hitAngle > 315f)) { PlaybackHaptics("Headshot_F"); }
                if ((hitAngle > 45f) && (hitAngle < 135f)) { PlaybackHaptics("Headshot_L"); }
                if ((hitAngle > 135f) && (hitAngle < 225f)) { PlaybackHaptics("Headshot_B"); }
                if ((hitAngle > 225f) && (hitAngle < 315f)) { PlaybackHaptics("Headshot_R"); }
            }
            else { PlayBackHit("BulletHit", hitAngle, 0.5f); }
        }

        public void PlayHolsterIn(bool isRight, bool isBack)
        {
            string key = "holster";
            if (isBack) key += "_shoulder";
            else key += "_hip";
            if (isRight) key += "_r";
            else key += "_l";
            PlaybackHaptics(key);
        }

        public void PlayHolsterOut(bool isRight, bool isBack)
        {
            string key = "unholster";
            if (isBack) key += "_shoulder";
            else key += "_hip";
            if (isRight) key += "_r";
            else key += "_l";
            PlaybackHaptics(key);
        }

        public void StartHeartBeat()
        {
            HeartBeat_mrse.Set();
        }

        public void StopHeartBeat()
        {
            HeartBeat_mrse.Reset();
        }

        public bool IsPlaying(String effect)
        {
            return BhapticsSDK2.IsPlaying(effect.ToLower());
        }

        public void StopHapticFeedback(String effect)
        {
            BhapticsSDK2.Stop(effect.ToLower());
        }

        public void StopAllHapticFeedback()
        {
            StopThreads();
            BhapticsSDK2.StopAll();
        }

        public void StopThreads()
        {
            StopHeartBeat();
        }


    }
}
