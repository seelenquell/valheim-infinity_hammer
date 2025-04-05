﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Argo.Blueprint;
using Argo.DataAnalysis;
using Argo.Zdo;
using Data;
using Microsoft.Win32.SafeHandles;
using ServerDevcommands;
using Service;
using UnityEngine;
using UnityEngine.Rendering;

namespace InfinityHammer;

public class HammerSaveCommandJson
{
    public HammerSaveCommandJson() {
        AutoComplete.Register( "hammer_save_json", (int index) => {
            if (index == 0) return ParameterInfo.Create( "File name." );
            return ["c", "center", "d", "data", "p", "profile", "s", "snap"];
        }, new() {
            { "c", (int       index) => ParameterInfo.ObjectIds },
            { "center", (int  index) => ParameterInfo.ObjectIds },
            { "d", (int       index) => ["true", "false"] },
            { "data", (int    index) => ["true", "false"] },
            { "p", (int       index) => ["true", "false"] },
            { "profile", (int index) => ["true", "false"] },
            { "s", (int       index) => ParameterInfo.ObjectIds },
            { "snap", (int    index) => ParameterInfo.ObjectIds },
        } );
        Helper.Command( "hammer_save_json",
            "[file name] [center=piece] [snap=piece] [data=true/false] [profile=true/false] - Saves the selection to a  json blueprint.",
            (args) => {
                HammerHelper.CheatCheck();
                Helper.ArgsCheck( args, 2, "Blueprint name is missing." );
                var                player         = Helper.GetPlayer();
                var                placementGhost = HammerHelper.GetPlacementGhost();
                Config config           = GetConfig(args);

                var name = Path.GetFileNameWithoutExtension( args[1] ) +
                    ".blueprint.json";
                var path =
                    Path.Combine(
                        config.SaveToProfile
                            ? Configuration.BlueprintLocalFolder
                            : Configuration.BlueprintGlobalFolder, name );
                Directory.CreateDirectory( Path.GetDirectoryName( path ) );
                //    File.Create(path);

                if (Selection.Get() is not ObjectSelection selection) {
                    throw new ArgumentException( "Selection error" );
                }
                Vector3 placement_pos = player.m_placementGhost.transform.position;
                try {
                    SelectedObjects selectedObjects
                        = new SelectedObjects( placementGhost, selection );
                    var bp = new BlueprintJson( player.GetPlayerName(), selectedObjects,
                        placement_pos,
                        config );
                    if (selection.Objects.Count() == 0) {
                        bp.BuildFromSelectionSingle();
                    }
                    // todo remove?
                    else {
                        bp.BuildFromSelection();
                    }
                    // todo write write funtion

                    bp.AddExportListener( (() => {
                        bp.WriteToFile( path );
                        args.Context.AddString(
                            $"Json Blueprint saved to {path.Replace( "\\", "\\\\" )}" +
                            $" (pos: {HammerHelper.PrintXZY( bp.Coordinates )} "      +
                            $"rot: {HammerHelper.PrintYXZ( bp.Rotation )})." );
                        Selection.CreateGhost( new ObjectSelection( args.Context,
                            bp,
                            Vector3.one ) );
                    }) );
                } catch (Exception e) {
                    System.Console.WriteLine( "Error inhammer_save_json " + e );
                }
            } );
    }
    public Config GetConfig(Terminal.ConsoleEventArgs args, string name = "", Config? cfg = null) {
        
        if (cfg == null) {
            if (name != "") {
                cfg = Config.GetDefault().Clone(name);
            }
            else {
                cfg = Config.GetDefault();
            }
        }
        cfg.CenterPiece   = Configuration.BlueprintCenterPiece;
        cfg.SnapPiece     = Configuration.BlueprintSnapPiece;
        cfg.SaveMode  = Configuration.SaveBlueprintData ? SaveExtraData.All : SaveExtraData.None;
        cfg.SaveToProfile       = Configuration.SaveBlueprintsToProfile;
        

        var pars  = args.Args.Skip( 2 ).ToArray();
        int index = 0;
        foreach (var par in pars) {
            var split = par.Split( '=' );
            if (split.Length < 2) {
                // Legacy support.
                if (index == 0)  cfg.CenterPiece = par;
                if (index == 1) cfg.SnapPiece    = par;
                continue;
            }

            if (split[0] == "center" || split[0] == "c")
                cfg.CenterPiece = split[1];
            if (split[0] == "snap" || split[0] == "s")
                cfg.SnapPiece  = split[1];
            if (split[0] == "data" || split[0] == "d") {
                bool? extradata = Parse.BoolNull( split[1] );
                if (extradata.HasValue) {
                    cfg.SaveMode  = extradata.Value ? SaveExtraData.All : SaveExtraData.None;
                }
            }
            if (split[0] == "profile" || split[0] == "p")
                cfg.SaveToProfile = Parse.BoolNull( split[1] ) ??
                    Configuration.SaveBlueprintsToProfile;
        }
        return cfg;
    }
}

