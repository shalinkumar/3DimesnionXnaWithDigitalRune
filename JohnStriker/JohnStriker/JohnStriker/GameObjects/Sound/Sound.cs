using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace JohnStriker.GameObjects.Sound
{
    class Sound
    {
        #region Variables
        /// <summary>
        /// Sound stuff for XAct
        /// </summary>
        static AudioEngine audioEngine;
        /// <summary>
        /// Wave bank
        /// </summary>
        static WaveBank waveBank;
        /// <summary>
        /// Sound bank
        /// </summary>
        static SoundBank soundBank;
        /// <summary>
        /// Default category to change volume of sounds.
        /// </summary>
        static AudioCategory defaultCategory;
        /// <summary>
        /// Gears category to change volume and pitching of gear sounds.
        /// </summary>
        static AudioCategory gearsCategory;
        /// <summary>
        /// Music category to change volume of music.
        /// </summary>
        static AudioCategory musicCategory;
        /// <summary>
        /// explosion category to change volume of music.
        /// </summary>
        static AudioCategory explosionCategory;

        private static AudioCategory passByCategory;
        #endregion

        #region Enums
        /// <summary>
        /// Sounds we use in this game. This are all the sounds and even the
        /// music, only the gear sounds are handled seperately below.
        /// </summary>
        /// <returns>Enum</returns>
        public enum Sounds
        {
            // Menu Sounds
            ButtonClick,
            ScreenClick,
            ScreenBack,
            Highlight,
            // Game Sounds
            Beep,
            Bleep,
            PassbyMajor,
            PassbyMinor,
            CarCrashMinor,
            GunAR15MachineGunSingle,
            Explosion,
            Missile,

            CarCrashTotal,
            Ammo,
            // Additional Game Sounds (gear sounds are extra)
            CheckpointBetter,
            CheckpointWorse,
            Victory,
            CarLose,
            // Music
            MenuMusic,
            GameMusic,
        }
        #endregion


        #region Constructor
        /// <summary>
        /// Private constructor to prevent instantiation.
        /// </summary>
        private Sound()
        {
        }

        #endregion

        public static void Initialize(ContentManager contentManager)
        {
            try
            {
                audioEngine = new AudioEngine(@"Content\AudioMyGame\MyGame.xgs");
                waveBank = new WaveBank(audioEngine, @"Content\AudioMyGame\Wave Bank.xwb");


                if (waveBank != null)
                {
                    soundBank = new SoundBank(audioEngine, @"Content\AudioMyGame\Sound Bank.xsb");
                }

                // Get the categories needed to change volume and pitching
                defaultCategory = audioEngine.GetCategory("Default");
                gearsCategory = audioEngine.GetCategory("Gears");
                musicCategory = audioEngine.GetCategory("Music");
                explosionCategory = audioEngine.GetCategory("Explosion");
                passByCategory = audioEngine.GetCategory("PassBy");
                //SetVolumes(GameSettings.Default.SoundVolume,
                //    GameSettings.Default.MusicVolume);

                SetVolumes(0.6f,
                  0.7f, 0.9f, 0.7f);
            }
            catch (NoAudioHardwareException ex)
            {
                // Is they have no Audio hardware, note it and move on. Surface any
                // other exception that occurs since something is actually wrong!
                //Log.Write("Failed to create sound class: " + ex.ToString());
            }
        }


        #region Play
        /// <summary>
        /// Play
        /// </summary>
        /// <param name="soundName">Sound name</param>
        public static void Play(string soundName)
        {
            if (soundBank == null)
                return;

            soundBank.PlayCue(soundName);
        }

        /// <summary>
        /// Play
        /// </summary>
        /// <param name="sound">Sound</param>
        public static void Play(Sounds sound)
        {
            Play(sound.ToString());
        }
        #endregion

        #region StopMusic
        /// <summary>
        /// Stop music
        /// </summary>
        public static void StopMusic()
        {
            if (soundBank == null)
                return;

            // Use a little trick, start new music, but use the cue. This will
            // replace the old music, then stop the music and everything is stopped!
            Cue musicCue = soundBank.GetCue("MenuMusic");
            musicCue.Play();
            // Wait for a short while to let Xact kick in ^^
            Thread.Sleep(10);
            musicCue.Stop(AudioStopOptions.Immediate);
        }
        #endregion


        #region PlayAmmoSound
        /// <summary>
        /// Prevent playing brake sounds on top of each other with help of this
        /// variable.
        /// </summary>
        static float ammoSoundStillPlayingMs = 2000;

        /// <summary>
        /// Play crash sound
        /// </summary>
        /// <param name="totalCrash">Total crash</param>
        public static void PlayAmmoSound(bool totalCrash)
        {
            Sound.Play(Sounds.GunAR15MachineGunSingle);
        }
        #endregion


        #region PlayExplosionSound
        /// <summary>
        /// Prevent playing brake sounds on top of each other with help of this
        /// variable.
        /// </summary>


        /// <summary>
        /// Play crash sound
        /// </summary>
        /// <param name="totalCrash">Total crash</param>
        public static void PlayExplosionSound(bool totalCrash)
        {
            Sound.Play(Sounds.Explosion);
        }
        #endregion


        #region PlayMissileSound

        /// <summary>
        /// Play crash sound
        /// </summary>
        /// <param name="totalCrash">Total crash</param>
        public static void PlayMissileSound(bool totalCrash)
        {
            Sound.Play(Sounds.Missile);
        }
        #endregion


        #region PlayPassbySound

        static Cue currentPassbyCue = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="soundPassbyType"></param>
        public static void PlayPassbySound(Sounds soundPassbyType)
        {
            //Sound.Play(soundPassbyType);
            currentPassbyCue = soundBank.GetCue(soundPassbyType.ToString());
            currentPassbyCue.Play();
        }

        #endregion


        #region Gear Constants
        /// <summary>
        /// Number of gears we got in this game.
        /// </summary>
        const int NumberOfGears = 5;



        /// <summary>
        /// All gear change sounds are 1200 ms long.
        /// </summary>
        const int GearChangeSoundLengthInMs = 1200;

        /// <summary>
        /// Constants for the gear sounds
        /// </summary>
        const float stayingVol = 0.5f;
        /// <summary>
        /// Volumes for each gear, currently all set to 100% because we
        /// adjusted the gear volumes in Xact already.
        /// </summary>
        static readonly float[] vol =
            new float[NumberOfGears] { 1, 1, 1, 1, 1 };
        /// <summary>
        /// Minimum pitch for each gear, this is the sound pitch you will hear
        /// when the gear is at the very start. It goes up to maxPitch and
        /// then the next gear is initiated.
        /// </summary>
        static readonly float[] minPitch =
            //very heavy:
            //new float[NumberOfGears] { -0.75f, -0.75f, -0.69f, -0.50f, -0.41f };
            //not so strong:
            new float[NumberOfGears] { -0.375f, -0.375f, -0.345f, -0.25f, -0.205f };

        /// <summary>
        /// Max pitch for each gear.
        /// </summary>
        static readonly float[] maxPitch =
            //very heavy:
            //new float[NumberOfGears] { 0.68f, 0.54f, 0.54f, 0.49f, 0.20f };
            //not so strong:
            //new float[NumberOfGears] { 0.34f, 0.27f, 0.27f, 0.245f, 0.10f };
            //even weaker, sounds better:
            new float[NumberOfGears] { 0.24f, 0.17f, 0.17f, 0.145f, 0.10f };


        #endregion

        #region Gear Variables


        static int currentGear = 0;
        #endregion

        #region PlayGearSound
        static Cue currentGearCue = null;
        static Cue currentGearChangeCue = null;
        static float gearChangeSoundInitiatedMs = 0;
        /// <summary>
        /// Play gear sound
        /// </summary>
        /// <param name="soundName">Gear sound type</param>
        private static void PlayGearSound(string soundName)
        {
            if (soundBank == null)
                return;

            if (soundName.Contains("To"))
            {
                // Gear sound is automatically replaced!
                currentGearChangeCue = soundBank.GetCue(soundName);
                currentGearChangeCue.Play();
                gearChangeSoundInitiatedMs = GearChangeSoundLengthInMs;
                currentGearCue = null;
            }
            else
            {
                // Gear change sound is automatically replaced!
                currentGearCue = soundBank.GetCue(soundName);
                currentGearCue.Play();
                currentGearChangeCue = null;
            }
        }
        #endregion

        #region Change gear volume and pitch
        /// <summary>
        /// Update gear volume and pitch
        /// </summary>
        /// <param name="gearSound">Gear sound</param>
        /// <param name="volume">Volume</param>
        /// <param name="pitch">pitch</param>
        private static void UpdateGearVolumeAndPitch(
            string gearSound, float volume, float pitch, TimeSpan timeSpan)
        {
            if (audioEngine == null)
                return;

            // Gear changing in progress?
            if (gearChangeSoundInitiatedMs > 0)
            {
                //gearChangeSoundInitiatedMs -=
                //    BaseGame.ElapsedTimeThisFrameInMilliseconds;

                gearChangeSoundInitiatedMs -= timeSpan.Milliseconds;

                // If gear change sound ends in this frame (max time - frameMs),
                // then start gear sound!
                if (gearChangeSoundInitiatedMs <= 0)
                {
                    gearChangeSoundInitiatedMs = 0;
                    PlayGearSound(gearSound);
                    volume = lastGearVolume = 1.0f;
                    pitch = lastGearPitch = -0.3f;
                }
            }

            // Set the global volume for this category
            //gearsCategory.SetVolume(MathHelper.Clamp(volume, 0, 1) *
            //    GameSettings.Default.SoundVolume);

            gearsCategory.SetVolume(MathHelper.Clamp(volume, 0, 1) *
              0.8f);

            // Set pitch only if this is a gear sound
            if (currentGearCue != null)
            {
                currentGearCue.SetVariable("Pitch",
                    55 * MathHelper.Clamp(pitch, -1, 1));
            }
        }
        #endregion




        #region Start gear sound
        /// <summary>
        /// Start gear sound
        /// </summary>
        public static void StartGearSound()
        {
            currentGear = 0;
            TimeSpan deltaTime = TimeSpan.Zero;
            Sound.PlayGearSound("Gear1");
            Sound.UpdateGearVolumeAndPitch("Gear1", stayingVol, minPitch[0], deltaTime);
        }
        #endregion

        #region Stop gear sound
        /// <summary>
        /// Start gear sound
        /// </summary>
        public static void StopGearSound()
        {
            // Stop everything
            currentGear = 0;
            if (currentGearChangeCue != null)
                currentGearChangeCue.Stop(AudioStopOptions.Immediate);
            currentGearChangeCue = null;
            if (currentGearCue != null)
                currentGearCue.Stop(AudioStopOptions.Immediate);
            currentGearCue = null;
        }
        #endregion

        #region Update gear sound
        static float lastGearVolume = stayingVol;
        static float lastGearPitch = 0;
        /// <summary>
        /// Update gear sound, must be called every frame to make sure we
        /// always have the most recent gear sounds. Especially the gear
        /// changing sounds depends on accuracity!
        /// </summary>
        public static void UpdateGearSound(float speed, float acceleration, TimeSpan timeSpan)
        {
            // Calculate new gear depending on the current speed
            //int newGear = (int)(NumberOfGears * speed / Player.MaxPossibleSpeed);

            int newGear = (int)(NumberOfGears * speed / 9.0);

            // Make sure newGear is between 0 and NumberOfGears
            if (newGear < 0)
                newGear = 0;
            if (newGear >= NumberOfGears)
                newGear = NumberOfGears - 1;



            // We can only change gear if no other gear change sound is in progress
            if (gearChangeSoundInitiatedMs <= 0)
            {




                if (newGear > currentGear)
                {
                    // Next gear
                    Sound.PlayGearSound(
                        "Gear" + (newGear) + "ToGear" + (newGear + 1));
                    lastGearVolume = 1.0f;
                    lastGearPitch = 0.0f;
                }
                else if (newGear < currentGear)
                {
                    // Previous gear, change immediately
                    //Sound.PlayGearSound(
                    //    "Gear" + (newGear + 2) + "ToGear" + (newGear + 1));
                    Sound.PlayGearSound("Gear" + (newGear + 1));
                    lastGearVolume = 1.0f;
                    lastGearPitch = maxPitch[newGear];
                }
                currentGear = newGear;



            }

            // If negative, play gear1 sound and make sure we stay in gear1
            if (speed < 0)
                //speed = MathHelper.Clamp(
                //    Math.Abs(speed), 0, Player.MaxPossibleSpeed / 5);
                speed = MathHelper.Clamp(
                    Math.Abs(speed), 0, 9.0f / 5);
            //float gearPercentage = (float)
            //    ((int)((speed / Player.MaxPossibleSpeed) * 499) %
            //    (int)(500 / NumberOfGears)) / 100.0f;
            float gearPercentage = (float)
              ((int)((speed / 9.0f) * 499) %
              (int)(500 / NumberOfGears)) / 100.0f;

            gearPercentage = MathHelper.Clamp(gearPercentage, 0, 1);



            float minVolume = currentGear > 0 ? vol[currentGear - 1] : stayingVol;
            float maxVolume = vol[currentGear];
            float volume = MathHelper.Lerp(minVolume, maxVolume, gearPercentage);
            float pitch = MathHelper.Lerp(
                minPitch[currentGear], maxPitch[currentGear], gearPercentage);

            // If gear change sound is in progress, make sure pitch is untouched
            if (gearChangeSoundInitiatedMs > 0)
            {
                pitch = 0;
            }

            // If accelerating use loud sounds.
            if (acceleration > 0.25f)
            {
                volume = 1.0f;
            }
            else
            {
                // If staying around or not accelerating, make a little quieter
                volume /= 1.75f;
                // If slowing down do not go above 0 for the pitch, sounds wrong!
                pitch = Math.Min(-0.025f, pitch / 1.25f);
                if (lastGearPitch > pitch)
                    lastGearPitch = lastGearPitch * 0.9f + pitch * 0.1f;
            }

            // Slowly interpolate volume and pitch, abrupt changes don't sound cool.
            // Always start with min/max pitch if we are in a new gear (see above).
            // Changes between gears and if accelerating or not should also be smooth.
            //lastGearVolume = MathHelper.Lerp(lastGearVolume, volume,
            //    5.0f * BaseGame.MoveFactorPerSecond);
            //lastGearPitch = MathHelper.Lerp(lastGearPitch, pitch,
            //    5.0f * BaseGame.MoveFactorPerSecond);

            lastGearVolume = MathHelper.Lerp(lastGearVolume, volume,
               5.0f * 0.1837478f);
            lastGearPitch = MathHelper.Lerp(lastGearPitch, pitch,
                5.0f * 0.1837478f);
            Sound.UpdateGearVolumeAndPitch(
                "Gear" + (currentGear + 1), lastGearVolume, lastGearPitch, timeSpan);

        }
        #endregion





        #region Update
        /// <summary>
        /// Update
        /// </summary>
        public static void Update(TimeSpan timeSpan)
        {

            //if (passbySoundStillPlayingMs > 0)
            //    passbySoundStillPlayingMs -= timeSpan.Milliseconds;
            //if (explosionSSoundStillPlayingMs > 0)
            //    explosionSSoundStillPlayingMs -= gameTime.ElapsedGameTime.Milliseconds;

            if (audioEngine != null)
                audioEngine.Update();
        }

        public static void SetVolumes(float soundVolume, float musicVolume, float explosionsVolume, float passByVolume)
        {
            if (audioEngine != null)
            {
                // Update sound volumes
                defaultCategory.SetVolume(soundVolume);
                musicCategory.SetVolume(musicVolume);
                explosionCategory.SetVolume(explosionsVolume);
                passByCategory.SetVolume(passByVolume);
                // Volume of gears is updated each frame
            }
        }
        #endregion
    }
}
