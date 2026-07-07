using Godot;
using System.Collections.Generic;
using System.Linq;

public static class SaveLoadManager
{
    private const string SaveDir = "user://save/";

    public static void SaveGame(GameManager gm, string slotName)
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SaveDir));

        var data = new Godot.Collections.Dictionary();

        // meta
        data["version"] = "0.1";
        data["seed"] = gm.State.Seed;
        data["year"] = gm.State.Year;
        data["xun"] = gm.State.Xun;
        data["player_sect_id"] = gm.State.PlayerSectId;
        data["next_disciple_id"] = gm.State.NextDiscipleId;

        // sects
        var sectsArr = new Godot.Collections.Array();
        foreach (var s in gm.State.Sects)
        {
            var sd = new Godot.Collections.Dictionary();
            sd["id"] = s.Id;
            sd["name"] = s.Name;
            sd["is_player"] = s.IsPlayer;
            sd["is_alive"] = s.IsAlive;
            sd["lingshi"] = s.Lingshi;
            sd["prestige"] = s.Prestige;
            sd["spirit_vein"] = s.SpiritVein;
            sd["meeting_hall"] = s.MeetingHall;
            sd["cultivation_room"] = s.CultivationRoom;
            sd["library"] = s.Library;
            sd["alchemy_room"] = s.AlchemyRoom;
            sd["spirit_field"] = s.SpiritField;
            sd["protection_array"] = s.ProtectionArray;
            var cityArr = new Godot.Collections.Array();
            foreach (var cid in s.ControlledCityIds) cityArr.Add(cid);
            sd["city_ids"] = cityArr;
            sd["personality"] = (int)s.Personality;
            sectsArr.Add(sd);
        }
        data["sects"] = sectsArr;

        // disciples
        var discArr = new Godot.Collections.Array();
        foreach (var d in gm.State.Disciples)
        {
            var dd = new Godot.Collections.Dictionary();
            dd["id"] = d.Id;
            dd["name"] = d.Name;
            dd["sect_id"] = d.SectId;
            dd["realm"] = (int)d.Realm;
            dd["sub_realm"] = d.SubRealm;
            dd["combat"] = d.Combat;
            dd["alchemy"] = d.Alchemy;
            dd["crafting"] = d.Crafting;
            dd["formation"] = d.Formation;
            dd["management"] = d.Management;
            dd["lifespan"] = d.Lifespan;
            dd["mood"] = d.Mood;
            dd["loyalty"] = d.Loyalty;
            dd["state"] = d.State;
            dd["cultivation_progress"] = d.CultivationProgress;
            discArr.Add(dd);
        }
        data["disciples"] = discArr;

        // locations
        var locArr = new Godot.Collections.Array();
        foreach (var l in gm.Locations)
        {
            var ld = new Godot.Collections.Dictionary();
            ld["id"] = l.Id;
            ld["name"] = l.Name;
            ld["type"] = (int)l.Type;
            ld["pos_x"] = l.Position.X;
            ld["pos_y"] = l.Position.Y;
            ld["population"] = l.Population;
            ld["owner_sect"] = l.OwnerSectId;
            ld["prosperity"] = l.Prosperity;
            ld["loyalty"] = l.Loyalty;
            ld["status"] = l.Status;
            ld["tax_base"] = l.TaxBase;
            // influence
            var infDict = new Godot.Collections.Dictionary();
            foreach (var kv in l.Influence)
                infDict[kv.Key.ToString()] = kv.Value;
            ld["influence"] = infDict;
            locArr.Add(ld);
        }
        data["locations"] = locArr;

        // armies
        var armyArr = new Godot.Collections.Array();
        foreach (var a in gm.Armies)
        {
            var ad = new Godot.Collections.Dictionary();
            ad["id"] = a.Id;
            ad["sect_id"] = a.SectId;
            ad["pos_x"] = a.Position.X;
            ad["pos_y"] = a.Position.Y;
            ad["order"] = (int)a.Order;
            ad["move_x"] = a.MoveTarget.X;
            ad["move_y"] = a.MoveTarget.Y;
            ad["attack_target"] = a.AttackTargetArmyId;
            var didArr = new Godot.Collections.Array();
            foreach (var did in a.DiscipleIds) didArr.Add(did);
            ad["disciple_ids"] = didArr;
            armyArr.Add(ad);
        }
        data["armies"] = armyArr;

        // wars
        var warArr = new Godot.Collections.Array();
        foreach (var w in gm.Wars)
        {
            if (w.Ended) continue;
            var wd = new Godot.Collections.Dictionary();
            wd["attacker"] = w.AttackerSectId;
            wd["defender"] = w.DefenderSectId;
            wd["score"] = w.WarScore;
            wd["turns"] = w.TurnsActive;
            wd["score_battles"] = w.ScoreFromBattles;
            wd["score_occ"] = w.ScoreFromOccupation;
            warArr.Add(wd);
        }
        data["wars"] = warArr;

        // relations
        var relArr = new Godot.Collections.Array();
        foreach (var r in gm.State.Relations)
        {
            var rd = new Godot.Collections.Dictionary();
            rd["a"] = r.SectA;
            rd["b"] = r.SectB;
            rd["favor"] = r.Favor;
            rd["fear"] = r.Fear;
            rd["trust"] = r.Trust;
            rd["state"] = (int)r.State;
            relArr.Add(rd);
        }
        data["relations"] = relArr;

        var path = SaveDir + slotName + ".json";
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (f != null)
        {
            f.StoreString(Json.Stringify(data, "\t"));
            GD.Print($"[Save] saved to {path}");
        }
    }

    public static bool LoadGame(GameManager gm, string slotName)
    {
        var path = SaveDir + slotName + ".json";
        if (!FileAccess.FileExists(path)) { GD.PrintErr($"[Save] no file: {path}"); return false; }

        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return false;

        var json = Json.ParseString(f.GetAsText());
        if (json.VariantType != Variant.Type.Dictionary) return false;
        var data = json.AsGodotDictionary();

        // restore state
        gm.State.Seed = (int)data["seed"];
        gm.State.Year = (int)data["year"];
        gm.State.Xun = (int)data["xun"];
        gm.State.PlayerSectId = (int)data["player_sect_id"];
        gm.State.NextDiscipleId = (int)data["next_disciple_id"];

        // sects
        gm.State.Sects.Clear();
        gm.State.SectMap.Clear();
        foreach (var item in (Godot.Collections.Array)data["sects"])
        {
            var sd = (Godot.Collections.Dictionary)item;
            var s = new SectData();
            s.Id = (int)sd["id"];
            s.Name = (string)sd["name"];
            s.IsPlayer = (bool)sd["is_player"];
            s.IsAlive = (bool)sd["is_alive"];
            s.Lingshi = (float)sd["lingshi"];
            s.Prestige = (float)sd["prestige"];
            s.SpiritVein = (float)sd["spirit_vein"];
            s.MeetingHall = (int)sd["meeting_hall"];
            s.CultivationRoom = (int)sd["cultivation_room"];
            s.Library = (int)sd["library"];
            s.AlchemyRoom = (int)sd["alchemy_room"];
            s.SpiritField = (int)sd["spirit_field"];
            s.ProtectionArray = (int)sd["protection_array"];
            s.Personality = (AIPersonality)(int)sd["personality"];
            var cityArr = (Godot.Collections.Array)sd["city_ids"];
            foreach (var cid in cityArr)
                s.ControlledCityIds.Add((int)cid);
            gm.State.Sects.Add(s);
            gm.State.SectMap[s.Id] = s;
        }

        // disciples
        gm.State.Disciples.Clear();
        foreach (var item in (Godot.Collections.Array)data["disciples"])
        {
            var dd = (Godot.Collections.Dictionary)item;
            var d = new DiscipleData((int)dd["id"], (string)dd["name"]);
            d.SectId = (int)dd["sect_id"];
            d.Realm = (Realm)(int)dd["realm"];
            d.SubRealm = (int)dd["sub_realm"];
            d.Combat = (int)dd["combat"];
            d.Alchemy = (int)dd["alchemy"];
            d.Crafting = (int)dd["crafting"];
            d.Formation = (int)dd["formation"];
            d.Management = (int)dd["management"];
            d.Lifespan = (int)dd["lifespan"];
            d.Mood = (int)dd["mood"];
            d.Loyalty = (int)dd["loyalty"];
            d.State = (string)dd["state"];
            d.CultivationProgress = (int)dd["cultivation_progress"];
            gm.State.Disciples.Add(d);
        }

        // locations
        gm.Locations.Clear();
        foreach (var item in (Godot.Collections.Array)data["locations"])
        {
            var ld = (Godot.Collections.Dictionary)item;
            var loc = new LocationData(
                (int)ld["id"], (string)ld["name"],
                (LocationType)(int)ld["type"],
                new Vector2((float)ld["pos_x"], (float)ld["pos_y"]),
                (int)ld["population"]);
            loc.OwnerSectId = (int)ld["owner_sect"];
            loc.Prosperity = (float)ld["prosperity"];
            loc.Loyalty = (float)ld["loyalty"];
            loc.Status = (string)ld["status"];
            loc.TaxBase = (int)ld["tax_base"];
            var infDict = (Godot.Collections.Dictionary)ld["influence"];
            foreach (var key in infDict.Keys)
                loc.Influence[(int)key] = (float)infDict[key];
            gm.Locations.Add(loc);
        }

        // armies
        gm.Armies.Clear();
        foreach (var item in (Godot.Collections.Array)data["armies"])
        {
            var ad = (Godot.Collections.Dictionary)item;
            var army = new ArmyData();
            army.Id = (int)ad["id"];
            army.SectId = (int)ad["sect_id"];
            army.Position = new Vector2((float)ad["pos_x"], (float)ad["pos_y"]);
            army.Order = (ArmyOrder)(int)ad["order"];
            army.MoveTarget = new Vector2((float)ad["move_x"], (float)ad["move_y"]);
            army.AttackTargetArmyId = (int)ad["attack_target"];
            var didArr = (Godot.Collections.Array)ad["disciple_ids"];
            foreach (var did in didArr)
                army.DiscipleIds.Add((int)did);
            army.ResolveDisciple = (id) => gm.State.Disciples.FirstOrDefault(d => d.Id == id);
            gm.Armies.Add(army);
        }

        // wars
        gm.Wars.Clear();
        foreach (var item in (Godot.Collections.Array)data["wars"])
        {
            var wd = (Godot.Collections.Dictionary)item;
            var war = new WarData();
            war.AttackerSectId = (int)wd["attacker"];
            war.DefenderSectId = (int)wd["defender"];
            war.WarScore = (int)wd["score"];
            war.TurnsActive = (int)wd["turns"];
            war.ScoreFromBattles = (int)wd["score_battles"];
            war.ScoreFromOccupation = (int)wd["score_occ"];
            gm.Wars.Add(war);
        }

        // relations
        gm.State.Relations.Clear();
        gm.State.RelationMap.Clear();
        foreach (var item in (Godot.Collections.Array)data["relations"])
        {
            var rd = (Godot.Collections.Dictionary)item;
            var rel = new RelationData((int)rd["a"], (int)rd["b"]);
            rel.Favor = (int)rd["favor"];
            rel.Fear = (int)rd["fear"];
            rel.Trust = (int)rd["trust"];
            rel.State = (RelationState)(int)rd["state"];
            gm.State.Relations.Add(rel);
            gm.State.RelationMap[(rel.SectA, rel.SectB)] = rel;
        }

        GD.Print($"[Save] loaded from {slotName}");
        return true;
    }

    public static string[] ListSaves()
    {
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(SaveDir)))
            return new string[0];
        var da = DirAccess.Open(SaveDir);
        if (da == null) return new string[0];
        var result = new System.Collections.Generic.List<string>();
        da.ListDirBegin();
        while (true)
        {
            var f = da.GetNext();
            if (string.IsNullOrEmpty(f)) break;
            if (f.EndsWith(".json"))
                result.Add(f.Replace(".json", ""));
        }
        da.ListDirEnd();
        return result.ToArray();
    }
}
