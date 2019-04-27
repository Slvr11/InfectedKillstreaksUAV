using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;

namespace UAVTest
{
    public class KillstreaksINF : BaseScript
    {
        private int Laser_FX;
        public int DeathstreaksAllowed = 0;

        public KillstreaksINF()
            : base()
        {
            Laser_FX = Call<int>("loadfx", "misc/laser_glow");
            if (Call<string>("getdvar", "mapname") == "mp_interchange") DeathstreaksAllowed = 1;
            else DeathstreaksAllowed = 0;

            PlayerConnected += new Action<Entity>(entity =>
            {
                entity.SetField("killstreak", 0);
                entity.SetField("deathstreak", 0);
                entity.SpawnedPlayer += () => CheckStreak(entity, "uav");
                entity.OnNotify("weapon_change", (player, newWeap) =>
                {
                    if (mayDropWeapon((string)newWeap))
                        entity.SetField("lastDroppableWeapon", (string)newWeap);
                    KillstreakUseWaiter(entity, (string)newWeap);
                });

                entity.OnNotify("weapon_fired", (ent, weaponName) =>
                {
                    if ((string)weaponName != "uav_strike_marker_mp")
                        return;

                    entity.AfterDelay(900, player => TakeUAVWeapon(entity));
                    AfterDelay(50, () =>
                        entity.SwitchToWeapon(ent.GetField<string>("lastDroppableWeapon")));

                    PrintNameInFeed(entity);

                    if (entity.GetField<string>("customStreak") == "uav")
                    {
                        entity.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, false);
                        entity.Call("SetPlayerData", "killstreaksState", "icons", 0, 0);
                        entity.SetField("customStreak", string.Empty);
                    }

                    Vector3 playerForward = ent.Call<Vector3>("gettagorigin", "tag_weapon") + Call<Vector3>("AnglesToForward", ent.Call<Vector3>("getplayerangles")) * 100000;

                    Entity refobject = Call<Entity>("spawn", "script_model", ent.Call<Vector3>("gettagorigin", "tag_weapon_left"));
                    refobject.Call("setmodel", "com_plasticcase_beige_big");
                    refobject.SetField("angles", ent.Call<Vector3>("getplayerangles"));
                    refobject.Call("moveto", playerForward, 100);
                    refobject.Call("hide");

                    refobject.OnInterval(10, (refent) =>
                    {
                        if (CollidingSoon(refent, ent))
                        {
                            Call("magicbullet", "uav_strike_projectile_mp", new Vector3(refent.Origin.X, refent.Origin.Y, refent.Origin.Z + 6000), refent.Origin, ent);

                            Entity redfx = Call<Entity>("spawnfx", Laser_FX, refent.Origin);
                            Call("triggerfx", redfx);
                            AfterDelay(4500, () => { redfx.Call("delete"); });
                            return false;
                        }

                        return true;
                    });
                });
            });
        }

        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (attacker.IsAlive && !player.IsAlive & attacker.GetField<string>("sessionteam") == "allies")
            {
                player.SetField("killstreak", 0);
                attacker.SetField("killstreak", attacker.GetField<int>("killstreak") + 1);
                player.SetField("deathstreak", player.GetField<int>("deathstreak") + 1);
                AfterDelay(500, () =>
                    {
                        CheckStreak(attacker, "uav");
                        CheckStreak(player, "uav");
                    });
            }
            else if (attacker.IsAlive && !player.IsAlive & attacker.GetField<string>("sessionteam") == "axis")
            {
                player.SetField("killstreak", 0);
                attacker.SetField("killstreak", attacker.GetField<int>("killstreak") + 1);
                attacker.SetField("deathstreak", 0);
                CheckStreak(attacker, "uav");
                CheckStreak(player, "uav");
            }
        }

        private bool CollidingSoon(Entity refobject, Entity player)
        {
            Vector3 endorigin = refobject.Origin + Call<Vector3>("anglestoforward", refobject.GetField<Vector3>("angles")) * 100;

            if (SightTracePassed(refobject.Origin, endorigin, false, player))
                return false;
            else
                return true;
        }
        private bool SightTracePassed(Vector3 StartOrigin, Vector3 EndOrigin, bool tracecharacters, Entity ignoringent)
        {
            int trace = Call<int>("SightTracePassed", new Parameter(StartOrigin), new Parameter(EndOrigin), tracecharacters, new Parameter(ignoringent));
            if (trace > 0)
                return true;
            else
                return false;
        }
        public void PrintNameInFeed(Entity player)
        {
            Call(334, string.Format("UAV Strike called in by {0}", player.GetField<string>("name")));
        }
        public void TakeUAVWeapon(Entity player)
        {
                player.TakeWeapon("uav_strike_marker_mp");
                player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
        }
        private void CheckStreak(Entity player, string streakName)
        {
            string wep = getKillstreakWeapon(streakName);
            if (string.IsNullOrEmpty(wep))
                return;

            if (player.GetField<int>("killstreak") == 15)
            {
                player.SetField("customStreak", streakName);

                player.Call(33392, "uav_strike", 0, 15);
                player.Call("giveWeapon", "uav_strike_marker_mp", 0, false);
                player.Call("setActionSlot", 4, "weapon", "uav_strike_marker_mp");
                player.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, true);
                player.Call("SetPlayerData", "killstreaksState", "icons", 0, getKillstreakIndex("predator_missile"));
                player.SetField("HasUAVStrike", 1);
            }
            if (player.GetField<int>("killstreak") == 30)
            {
                player.SetField("customStreak", "emp");

                player.Call(33392, "explosive_ammo", 0, 30);
                player.Call("giveWeapon", "killstreak_emp_mp", 0, false);
                player.Call("setActionSlot", 4, "weapon", "killstreak_emp_mp");
                player.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, true);
                player.Call("SetPlayerData", "killstreaksState", "icons", 0, getKillstreakIndex("_specialty_blastshield_ks"));
                player.SetField("HasExpBullets", 1);
            }
            if (player.GetField<int>("killstreak") == 20)
            {
                player.SetField("customStreak", streakName);
                player.SetField("AA12Ammo", 0);
                Call(334, string.Format("{0} has the Auto-Shotgun!", player.GetField<string>("name")));
                player.Call(33392, "auto_shotgun", 0, 20);
                player.Call("giveWeapon", "iw5_aa12_mp", 0, false);
                player.Call("setActionSlot", 4, "weapon", "iw5_aa12_mp");
                player.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, true);
                player.Call("SetPlayerData", "killstreaksState", "icons", 0, getKillstreakIndex("sentry"));
                player.SetField("HasShotgun", 1);
            }
            if (player.GetField<int>("deathstreak") > 14 && player.Call<int>(48, "infDStreaksAllowed") == 1)
            {
                player.SetField("customStreak", streakName);

                player.Call("giveWeapon", "killstreak_helicopter_mp", 0, false);
                player.Call("setActionSlot", 4, "weapon", "killstreak_helicopter_mp");
                HudElem KS = HudElem.CreateFontString(player, "hudsmall", 1);
                KS.SetPoint("CENTER", "CENTER", 0, -150);
                KS.SetText("Press ^3[{+actionslot 4}] ^7for Throwing Knife Deathstreak");
                OnInterval(100, () =>
                    {
                        if (player.CurrentWeapon == "killstreak_helicopter_mp")
                        {
                            KS.Call("destroy");
                            return false;
                        }
                        else return true;
                    });
            }
            else
                return;
        }
        private string getKillstreakWeapon(string streakName)
        {
            string ret = string.Empty;
            ret = Call<string>("tableLookup", "mp/killstreakTable.csv", 1, streakName, 12);
            return ret;
        }
        private int getKillstreakIndex(string streakName)
        {
            int ret = 0;
            ret = Call<int>("tableLookupRowNum", "mp/killstreakTable.csv", 1, streakName) - 1;

            return ret;
        }
        private void KillstreakUseWaiter(Entity ent, string weapon)
        {
            if (weapon == "uav_strike_marker_mp")
            {
                var elem = HudElem.CreateFontString(ent, "hudlarge", 2.5f);
                elem.SetPoint("BOTTOMCENTER", "BOTTOMCENTER", 0, -60);
                elem.SetText("Lase target for Predator Strike.");
                ent.AfterDelay(3500, player => elem.Call("destroy"));
            }
            if (weapon == "killstreak_emp_mp")
            {
                ent.AfterDelay(750, entity =>
                    ent.TakeWeapon("killstreak_emp_mp"));
                AfterDelay(50, () =>
                        ent.SwitchToWeapon(ent.GetField<string>("lastDroppableWeapon")));
                AfterDelay(50, () =>
                ent.SwitchToWeapon(ent.GetField<string>("lastDroppableWeapon")));
                ent.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, false);
                ent.Call("SetPlayerData", "killstreaksState", "icons", 0, 0);
                ent.SetField("customStreak", string.Empty);
                Call(334, ent.Name + " has Explosive Bullets!");
                ent.SetPerk("specialty_explosivebullets", true, true);
                ent.Call("iPrintLnBold", string.Format("30 seconds left for Explosive Bullets."));
                ent.AfterDelay(21000, entity =>
                    ent.Call("iPrintLnBold", string.Format("10 seconds left for Explosive Bullets!")));
                ent.AfterDelay(31000, entity =>
                    ent.Call("iPrintLnBold", string.Format("Explosive Bullets over!")));
                ent.AfterDelay(31010, entity =>
                    ent.SetPerk("specialty_explosivebullets", false, false));
                ent.AfterDelay(31015, entity =>
                    ent.Call("unsetperk", "specialty_explosivebullets", true));
            }
            if (weapon == "iw5_aa12_mp")
            {
                ent.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, false);
                ent.Call("SetPlayerData", "killstreaksState", "icons", 0, 0);
                ent.SetField("customStreak", string.Empty);
                if (ent.GetField<int>("AA12Ammo") == 0)
                {
                    ent.Call("iPrintLnBold", string.Format("Auto-Shotgun Activated!"));
                    ent.Call("setweaponammostock", "iw5_aa12_mp", 32);
                    ent.SetField("AA12Ammo", 1);
                }
                ent.OnNotify("weapon_fired", (e, _weapon) =>
                {
                    var currentWeapon = ent.CurrentWeapon;
                    if (ent.GetWeaponAmmoStock("aa12_mp") == 0 && ent.GetWeaponAmmoStock("aa12_mp") == 0 && ent.Call<int>("hasweapon", "aa12_mp") == 0)
                        ent.TakeWeapon("iw5_aa12_mp");
                    AfterDelay(50, () =>
                        ent.SwitchToWeapon(ent.GetField<string>("lastDroppableWeapon")));
                });
            }
            if (weapon == "killstreak_helicopter_mp")
            {
                ent.AfterDelay(1300, entity =>
                    ent.TakeWeapon("killstreak_helicopter_mp"));
                AfterDelay(50, () =>
                ent.SwitchToWeapon(ent.GetField<string>("lastDroppableWeapon")));
                ent.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, false);
                ent.Call("SetPlayerData", "killstreaksState", "icons", 0, 0);
                ent.SetField("customStreak", string.Empty);
                Call(334, ent.Name + " has acquired a Throwing Knife!");
                ent.Call("iPrintLnBold", "You have acquired a ^2Throwing Knife^7.");
                ent.Call(33541, "throwingknife");
                ent.GiveWeapon("throwingknife_mp");
                ent.Call("setweaponammoclip", "throwingknife_mp", 1);
            }
        }
        private bool mayDropWeapon(string weapon)
        {
            if (weapon == "none")
                return false;

            if (weapon.Contains("ac130"))
                return false;

            string invType = Call<string>("WeaponInventoryType", weapon);
            if (invType != "primary")
                return false;

            return true;
        }
    }
}