﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Storm;
using Storm.ExternalEvent;
using Storm.StardewValley.Event;

namespace RegenMod
{
    [Mod]
    public class RegenMod : DiskResource
    {
        public static RegenConfig ModConfig { get; private set; }
        public static float HealthFloat { get; private set; }
        public static float StaminaFloat { get; private set; }

        [Subscribe]
        public void InitializeCallback(InitializeEvent @event)
        {
            ModConfig = new RegenConfig();
            ModConfig = (RegenConfig)Config.InitializeConfig(Config.GetBasePath(this), ModConfig);

            Console.WriteLine("RegenMod by Zoryn => Initialization Completed");
        }

        [Subscribe]
        public void UpdateCallback(PreUpdateEvent @event)
        {
            if (@event.LocalPlayer.Expose() == null)
                return;

            if (ModConfig.RegenHealth)
            {
                if (!ModConfig.RegenHealthOnlyWhileStill || (ModConfig.RegenHealthOnlyWhileStill && @event.LocalPlayer.TimerSinceLastMovement > 1000))
                {
                    HealthFloat += ModConfig.RegenHealthPerSecond * (float) (@event.Root.CurrentGameTime.ElapsedGameTime.TotalMilliseconds / 1000);

                    if (HealthFloat > 1)
                    {
                        @event.LocalPlayer.Health = @event.LocalPlayer.Health >= @event.LocalPlayer.MaxHealth ? @event.LocalPlayer.MaxHealth : @event.LocalPlayer.Health + 1;
                        HealthFloat -= 1;
                    }
                }
            }
            
            if (ModConfig.RegenStamina)
            {
                if (!ModConfig.RegenStaminaOnlyWhileStill || (ModConfig.RegenStaminaOnlyWhileStill && @event.LocalPlayer.TimerSinceLastMovement > 1000))
                {
                    StaminaFloat += ModConfig.RegenStaminaPerSecond * (float) (@event.Root.CurrentGameTime.ElapsedGameTime.TotalMilliseconds / 1000);

                    if (StaminaFloat > 1)
                    {
                        @event.LocalPlayer.Stamina = @event.LocalPlayer.Stamina >= @event.LocalPlayer.MaxStamina ? @event.LocalPlayer.MaxStamina : @event.LocalPlayer.Stamina + 1;
                        StaminaFloat -= 1;
                    }
                }
            }
        }

        [Subscribe]
        public void ChatMessageEnteredCallback(ChatMessageEnteredEvent @event)
        {
            Command c = Command.ParseCommand(@event.ChatText);
            if (c.Name == "rlcfg" && c.HasArgs && (c.Args[0] == "regenmod" || c.Args[0] == "all"))
            {
                Console.WriteLine("Reloading the config for RegenMod by Zoryn");
                ModConfig = new RegenConfig();
                ModConfig = (RegenConfig)Config.InitializeConfig(Config.GetBasePath(this), ModConfig);
            }
        }
    }

    public class RegenConfig : Config
    {
        public bool RegenStamina { get; set; }
        public bool RegenStaminaOnlyWhileStill { get; set; }
        public float RegenStaminaPerSecond { get; set; }

        public bool RegenHealth { get; set; }
        public bool RegenHealthOnlyWhileStill { get; set; }
        public float RegenHealthPerSecond { get; set; }

        public override Config GenerateBaseConfig(Config baseConfig)
        {
            RegenStamina = false;
            RegenStaminaOnlyWhileStill = false;
            RegenStaminaPerSecond = 0.25f;

            RegenHealth = false;
            RegenHealthOnlyWhileStill = false;
            RegenHealthPerSecond = 0.25f;

            return this;
        }
    }
}
