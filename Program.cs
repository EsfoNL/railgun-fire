using Microsoft.Build.Framework;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{



    partial class Program : MyGridProgram
    {


        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.



        public Program()
        {
            
            var linesArray = Me.CustomData.Split(
                '\n'
            );
            // Echo(linesArray[0].ToString());
            if (linesArray.Length % 2 == 0 || linesArray.Length == 0) {
                Echo(
                    "incorrect amount of groups in custom data, content is supposed to be formatted like this:\n" +
                    "<base railgun group>\n" +
                    "<piston group 1>\n" +
                    "<railgun group 1\n" +
                    "...\n" +
                    "<piston group n>\n" +
                    "<railgun group n>\n"
                );
                return;
            }

            var lines = linesArray.AsEnumerable().GetEnumerator();
            lines.Reset();
            lines.MoveNext();

            var list = new List<IMySmallMissileLauncherReload>();

            var basegroup = GridTerminalSystem.GetBlockGroupWithName(lines.Current);
            if (basegroup == null) {
                throw new Exception($"Group {lines.Current} does not exist, aborting");
            }
            basegroup.GetBlocksOfType(list);
            

            pistons.Add(new List<IMyPistonBase>());
            railguns.Add(list);


            while (lines.MoveNext()) {
                var pistonGroupName = lines.Current;
                lines.MoveNext();
                var railgunGroupName = lines.Current;
                
                var pistonList = new List<IMyPistonBase>();
                var railgunList = new List<IMySmallMissileLauncherReload>();

                var pistonGroup = GridTerminalSystem.GetBlockGroupWithName(pistonGroupName);
                if (pistonGroup == null) {
                    throw new Exception($"Group {pistonGroupName} does not exist, aborting");
                }
                pistonGroup.GetBlocksOfType(pistonList);
                var railgunGroup = GridTerminalSystem.GetBlockGroupWithName(railgunGroupName);
                if (railgunGroup == null) {
                    throw new Exception($"Group {railgunGroupName} does not exist, aborting");
                }

                railgunGroup.GetBlocksOfType(railgunList);

                pistons.Add(pistonList);
                railguns.Add(railgunList);
            }

            Echo(
                string.Join(
                    ",",
                    pistons.SelectMany(v => v.Select(z => z.CustomName).ToArray())
                )
            );

            Echo(
                string.Join(
                    ",",
                    railguns.SelectMany(v => v.Select(z => z.CustomName).ToArray())
                )
            );           
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
        }

        List<List<IMyPistonBase>> pistons = new List<List<IMyPistonBase>>();
        // idk why this class either
        List<List<IMySmallMissileLauncherReload>> railguns = new List<List<IMySmallMissileLauncherReload>>();

        bool firing = false;
        bool auto = true;
        bool skip = false;
        int selectedGroupIndex = 0;

        int railgunToFireIndex = 0;



        public void Main(string argument, UpdateType updateSource)
        {
            const float Epsilon = 0.01f;
            
            if (updateSource == UpdateType.Terminal)
            {
                switch (argument) {
                    case "next":
                        selectedGroupIndex = (selectedGroupIndex + 1) % railguns.Count;
                        railgunToFireIndex = 0;
                        firing = false;
                        break;
                    case "fire":
                        firing = true;
                        break;
                    case "fire-next":
                        selectedGroupIndex = (selectedGroupIndex + 1) % railguns.Count;
                        railgunToFireIndex = 0;
                        firing = true;
                        break;
                    case "stop":
                        firing = false;
                        auto = false;
                        break;
                    case "auto":
                        auto = true;
                        firing = true;
                        break;
                }
            }

            if (skip) {
                skip = false;
                return;
            }

            foreach (var i in pistons[selectedGroupIndex])
            {
                if (i.Velocity > 0)
                {
                    i.Reverse();
                }
            }

            foreach (var piston in pistons.Where((_, i) => i != selectedGroupIndex).SelectMany(v => v))
            {
                if (piston.Velocity < 0)
                {
                    piston.Reverse();
                }
            }

            bool correctPosition =
                pistons
                    .Where((_, i) => i != selectedGroupIndex)
                    .SelectMany(v => v)
                    .All((v) => v.CurrentPosition > v.MaxLimit - Epsilon)
                && pistons[selectedGroupIndex].All((v) => v.CurrentPosition - Epsilon < 0);

            // check piston statuses
            if (correctPosition && firing)
            {
                railguns[selectedGroupIndex][railgunToFireIndex].ShootOnce();
                railgunToFireIndex++;
                if (railgunToFireIndex >= railguns[selectedGroupIndex].Count)
                {
                    railgunToFireIndex = 0;
                    if (auto) {
                        skip = true;
                        selectedGroupIndex = (selectedGroupIndex + 1) % railguns.Count;
                    } else {
                        firing = false;
                    }
                }
            }


            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
        }
    }
}
