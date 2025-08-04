//
// using System;
// using HarmonyLib;
// using Pigeon.Movement;
// using UnityEngine;
//
// namespace MoreAndQuickLoadouts;
//
// public class NotAnotherTentacleShit : MonoBehaviour
// {
//     public enum LoadoutType
//     {
//         Weapon,
//         Grenade,
//         Employee
//     }
//
//     private bool _enableLoadoutWheel;
//     private float _enableLoadoutWheelTime;
//     private int _selectedLoadoutIndex;
//     
//     private LoadoutData[] _data;
//
//     private void ToggleLoadoutWheel(bool performed, LoadoutType type)
//     {
//         if (performed)
//         {
//             _enableLoadoutWheel = true;
//             _enableLoadoutWheelTime = Time.unscaledTime;
//             _selectedLoadoutIndex = 0;
//             switch (type)
//             {
//                 case LoadoutType.Weapon:
//                 {
//                     var weapon = PlayerData.GetGearData(Player.LocalPlayer.SelectedGear);
//                     var fieldRefLoadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
//                     if (fieldRefLoadouts(weapon) is Array { Length: > 0 } loadouts)
//                     {
//                         for (var i = 0; i < loadouts.Length; i++)
//                         {
//                             _data[i] = new LoadoutData()
//                             {
//                                 GearData = weapon,
//                                 Icon = weapon.GetLoadoutIcon(i),
//                                 Label = $"Loadout {i + 1}",
//                                 LoadoutIndex = i
//                             };
//                         }
//                         SetupLoadouts(_data);
//                     }
//                     
//                 }
//             break;
//                 case LoadoutType.Grenade:
//                     
//                     break;
//                 case LoadoutType.Employee:
//                     break;
//             }
//         }
//         else
//         {
//             if (gameObject.activeSelf)
//             {
//                 PlayerInput.DisableMenu();
//                 gameObject.SetActive(false);
//                 --PlayerLook.Instance.RotationLocksX;
//                 --PlayerLook.Instance.RotationLocksY;
//                 Player.LocalPlayer.LockFiring(false);
//             }
//             _enableLoadoutWheel = false;
//             ApplyLoadout(type, _data[_selectedLoadoutIndex].LoadoutIndex);
//         }
//     }
//
//     private void ApplyLoadout(LoadoutType type, int loadoutIndex)
//     {
//         switch (type)
//         {
//             case LoadoutType.Weapon:
//             {
//                 var weapon = PlayerData.GetGearData(Player.LocalPlayer.SelectedGear);
//                 weapon.EquipLoadout(loadoutIndex);
//             }
//                 break;
//             case LoadoutType.Grenade:
//                     
//                 break;
//             case LoadoutType.Employee:
//                 break;
//         }
//     }
//
//     private void SetupLoadouts(LoadoutData[] data)
//     {
//         
//     }
// }
//
// public struct LoadoutData
// {
//     public string Label;
//     public Sprite Icon;
//     public int LoadoutIndex;
//     public PlayerData.GearData GearData;
// }