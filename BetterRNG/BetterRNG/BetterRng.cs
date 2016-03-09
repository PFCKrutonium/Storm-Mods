﻿using System;
using System.Collections.Generic;
using System.Linq;
using Storm;
using Storm.ExternalEvent;
using Storm.StardewValley;
using Storm.StardewValley.Accessor;
using Storm.StardewValley.Event;
using Storm.StardewValley.Wrapper;

namespace BetterRNG
{
    [Mod]
    public class BetterRng : DiskResource
    {
        public static MersenneTwister Twister { get; private set; }
        public static float[] RandomFloats { get; private set; }

        public static RngConfig ModConfig { get; private set; }
        public static bool JustLoadedGame { get; private set; }

        #region Fishing

        public static bool BeganFishingGame { get; protected set; }
        public static int UpdateIndex { get; protected set; }
        public static bool HitZero { get; protected set; }
        public IEnumerable<ProportionValue<Int32>> OneToFive { get; protected set; }

        public ClickableMenu ActiveMenu => StaticGameContext.WrappedGame.ActiveClickableMenu;

        public BobberBarAccessor BobberAcc => (BobberBarAccessor)StaticGameContext.WrappedGame.ActiveClickableMenu.Expose();

        public BobberBar Bobber => new BobberBar(StaticGameContext.WrappedGame, BobberAcc);

        #endregion

        [Subscribe]
        public void InitializeCallback(InitializeEvent @event)
        {
            ModConfig = new RngConfig();
            ModConfig = (RngConfig)Config.InitializeConfig(Config.GetBasePath(this), ModConfig);
            RandomFloats = new float[256];
            Twister = new MersenneTwister();

            //Destroys the game's built-in random number generator for Twister.
            @event.Root.Random = Twister;

            //Just fills the buffer with junk so that we know everything is good and random.
            RandomFloats.FillFloats();

            //Determine base RNG to get everything up and running.
            DetermineRng(@event);

            OneToFive = new[] {ProportionValue.Create(80, 1), ProportionValue.Create(15, 2), ProportionValue.Create(3, 3), ProportionValue.Create(1.9f, 4), ProportionValue.Create(0.1f, 5)};

            Console.WriteLine("BetterRng by Zoryn => Initialization Completed");
        }

        #region Daily RNG

        [Subscribe]
        public void AfterGameLoadedCallback(AfterGameLoadedEvent @event)
        {
            JustLoadedGame = true;
        }

        [Subscribe]
        public void PlayMorningSongCallback(PlayMorningSongEvent @event)
        {
            //Loading is async for some reason... so we'll just keep track that we initiated loading and then when this event fires trigger the rng manipulation
            if (JustLoadedGame)
            {
                DetermineRng(@event);
                JustLoadedGame = false;
            }
        }

        [Subscribe]
        public void AfterNewDayCallback(AfterNewDayEvent @event)
        {
            DetermineRng(@event);
        }

        public static void DetermineRng(StaticContextEvent @event)
        {
            //0 = SUNNY, 1 = RAIN, 2 = CLOUDY/SNOWY, 3 = THUNDER STORM, 4 = FESTIVAL/EVENT/SUNNY, 5 = SNOW
            //Generate a good set of new random numbers to choose from for daily luck every morning.
            RandomFloats.FillFloats();
            @event.Root.DailyLuck = RandomFloats.Random() / 10;

            float[] weatherConfig = new[] { ModConfig.SunnyChance, ModConfig.CloudySnowyChance, ModConfig.RainyChance, ModConfig.StormyChance, ModConfig.HarshSnowyChance };
            if (weatherConfig.Sum() >= 0.99f && weatherConfig.Sum() <= 1.01f)
            {
                var floats = new[] { ProportionValue.Create(ModConfig.SunnyChance, 0), ProportionValue.Create(ModConfig.CloudySnowyChance, 2), ProportionValue.Create(ModConfig.RainyChance, 1), ProportionValue.Create(ModConfig.StormyChance, 3), ProportionValue.Create(ModConfig.HarshSnowyChance, 5) };
                @event.Root.WeatherForTomorrow = floats.ChooseByRandom();
            }
            else
                Console.WriteLine("Could not set weather because the config values do not add up to 1.0 ({0}).\n\tPlease correct this error in: " + ModConfig.ConfigLocation, weatherConfig.Sum());

            //Console.WriteLine("[Twister] Daily Luck: " + @event.Root.DailyLuck + " | Tomorrow's Weather: " + @event.Root.WeatherForTomorrow);
        }

        #endregion

        #region FishingRng

       

        public void PreUpdateCallback(PreUpdateEvent @event)
        {
            if (!ModConfig.EnableFishingTreasureOverride && !ModConfig.EnableFishingStuffOverride)
                return;

            if (ActiveMenu == null)
                return;

            if (ActiveMenu.IsBobberBar() && !HitZero)
            {
                //Begin fishing game
                if (!BeganFishingGame && UpdateIndex > 20)
                {
                    //Do these things once per fishing minigame, 1/3 second after it updates
                    //This will override anything from the FishingMod by me, and from any other mod that modifies these things before this

                    if (ModConfig.EnableFishingTreasureOverride)
                        Bobber.Treasure = Twister.NextComplexBool(RandomFloats);

                    if (ModConfig.EnableFishingStuffOverride)
                    {
                        Bobber.Difficulty = Twister.Next(15, 125);
                        Bobber.MinFishSize = (int)Math.Round(Bobber.MinFishSize * RandomFloats.Random().Abs());
                        Bobber.MaxFishSize = (int)Math.Round(Bobber.MinFishSize * (OneToFive.ChooseByRandom() + RandomFloats.Random().Abs()));
                        Bobber.FishSize = Twister.Next(Bobber.MinFishSize, Bobber.MaxFishSize);
                    }

                    BeganFishingGame = true;
                }

                if (UpdateIndex < 20)
                    UpdateIndex++;

                if (Bobber.DistanceFromCatching <= 0.05f)
                    HitZero = true;
            }
            else
            {
                //End fishing game
                BeganFishingGame = false;
                UpdateIndex = 0;
                HitZero = false;
            }
        } 

        #endregion

        [Subscribe]
        public void ChatMessageEnteredCallback(ChatMessageEnteredEvent @event)
        {
            Command c = Command.ParseCommand(@event.ChatText);
            if (c.Name == "rlcfg" && c.HasArgs && (c.Args[0] == "betterrng" || c.Args[0] == "all"))
            {
                Console.WriteLine("Reloading the config for BetterRNG by Zoryn");
                ModConfig = new RngConfig();
                ModConfig = (RngConfig)Config.InitializeConfig(Config.GetBasePath(this), ModConfig);
            }
        }
    }

    public class RngConfig : Config
    {
        public bool EnableDailyLuckOverride { get; set; }
        public bool EnableWeatherOverride { get; set; }
        public float SunnyChance { get; set; }
        public float CloudySnowyChance { get; set; }
        public float RainyChance { get; set; }
        public float StormyChance { get; set; }
        public float HarshSnowyChance { get; set; }

        public bool EnableFishingTreasureOverride { get; set; }
        public float FishingTreasureChance { get; set; }
        public bool EnableFishingStuffOverride { get; set; }

        public override Config GenerateBaseConfig(Config baseConfig)
        {
            EnableDailyLuckOverride = true;
            EnableWeatherOverride = true;
            SunnyChance = 0.60f;
            CloudySnowyChance = 0.15f;
            RainyChance = 0.15f;
            StormyChance = 0.05f;
            HarshSnowyChance = 0.05f;

            EnableFishingTreasureOverride = true;
            FishingTreasureChance = 1 / 16f;
            EnableFishingStuffOverride = true;
            return this;
        }
    }


    public static class Extensions
    {
        public static float[] DynamicDowncast(this Byte[] bytes)
        {
            float[] f = new float[bytes.Length / 4];
            for (int i = 0; i < f.Length; i++)
            {
                f[i] = BitConverter.ToSingle(bytes, i == 0 ? 0 : (i * 4) - 1);
            }
            return f;
        }

        public static void FillFloats(this float[] floats)
        {
            for (int i = 0; i < floats.Length; i++)
                floats[i] = BetterRng.Twister.Next(-100,100) / 100f;
        }

        public static T Random<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            var list = enumerable as IList<T> ?? enumerable.ToList();
            return list.Count == 0 ? default(T) : list[BetterRng.Twister.Next(0, list.Count)];
        }

        public static float Abs(this float f)
        {
            return Math.Abs(f);
        }

        public static T ChooseByRandom<T>(this IEnumerable<ProportionValue<T>> collection)
        {
            var rnd = BetterRng.Twister.NextDouble();
            foreach (var item in collection)
            {
                if (rnd < item.Proportion)
                    return item.Value;
                rnd -= item.Proportion;
            }
            throw new InvalidOperationException("The proportions in the collection do not add up to 1.");
        }
    }

    public class ProportionValue<T>
    {
        public double Proportion { get; set; }
        public T Value { get; set; }

        
    }

    public static class ProportionValue
    {
        public static ProportionValue<T> Create<T>(double proportion, T value)
        {
            return new ProportionValue<T> { Proportion = proportion, Value = value };
        }
    }
}
