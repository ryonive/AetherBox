using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AetherBox.Helpers;
// matches FFXIVClientStructs.FFXIV.Client.Game.ActionType
public enum ActionType : byte
{
    None = 0,
    Spell = 1,
    Item = 2,
    KeyItem = 3,
    Ability = 4,
    General = 5,
    Companion = 6,
    CraftAction = 9,
    MainCommand = 10,
    PetAction = 11,
    Mount = 13,
    PvPAction = 14,
    Waymark = 15,
    ChocoboRaceAbility = 16,
    ChocoboRaceItem = 17,
    SquadronAction = 19,
    Accessory = 20
}