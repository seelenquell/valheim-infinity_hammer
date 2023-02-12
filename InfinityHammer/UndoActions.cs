using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace InfinityHammer;

[HarmonyPatch(typeof(ZNetView), "Awake")]
public static class UndoTracker
{
  public static void Postfix(ZNetView __instance)
  {
    if (Undo.Track)
      Undo.CreateObject(__instance.gameObject);
  }
}
public class Undo
{
  private static BindingFlags PrivateBinding = BindingFlags.Static | BindingFlags.NonPublic;
  private static Type? Type() => CommandWrapper.ServerDevcommands?.GetType("ServerDevcommands.UndoManager");

  public static void Place(IEnumerable<ZDO> objs)
  {
    if (objs.Count() == 0) return;
    UndoPlace action = new(objs);
    Type()?.GetMethod("Add", PrivateBinding).Invoke(null, new[] { action });
  }
  public static void Remove(IEnumerable<ZDO> objs)
  {
    if (objs.Count() == 0) return;
    UndoRemove action = new(objs);
    Type()?.GetMethod("Add", PrivateBinding).Invoke(null, new[] { action });
  }
  private static bool GroupCreating = false;
  public static List<ZDO> Objects = new();
  public static bool Track = false;
  public static void CreateObject(GameObject obj)
  {
    if (!obj) return;
    foreach (var view in obj.GetComponentsInChildren<ZNetView>())
    {
      if (view.GetZDO() != null)
        Objects.Add(view.GetZDO());
    }
    if (!GroupCreating && !Track) Finish();
  }
  public static void StartTracking()
  {
    Track = true;
  }
  public static void StopTracking()
  {
    Track = false;
    if (!GroupCreating && !Track) Finish();
  }
  public static void StartCreating()
  {
    GroupCreating = true;
  }
  public static void FinishCreating()
  {
    GroupCreating = false;
    if (!GroupCreating && !Track) Finish();
  }
  private static void Finish()
  {
    Place(Objects);
    Objects.Clear();
    GroupCreating = false;
    Track = false;
  }
  public static ZDO Place(ZDO zdo)
  {
    DataHelper.Init(zdo);
    var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
    if (!prefab) throw new InvalidOperationException("Invalid prefab");
    var obj = UnityEngine.Object.Instantiate<GameObject>(prefab, zdo.GetPosition(), zdo.GetRotation());
    var netView = obj.GetComponent<ZNetView>();
    if (!netView) throw new InvalidOperationException("No view");
    return netView.GetZDO();
  }
  public static ZDO[] Place(ZDO[] data) => data.Select(Place).Where(obj => obj != null).ToArray();

  public static string Name(ZDO zdo) => Utils.GetPrefabName(ZNetScene.instance.GetPrefab(zdo.GetPrefab()));
  public static string Print(ZDO[] data)
  {
    if (data.Count() == 1) return Name(data.First());
    var names = data.GroupBy(Name);
    if (names.Count() == 1) return $"{names.First().Key} {names.First().Count()}x";
    return $" objects {data.Count()}x";
  }
  public static ZDO[] Remove(ZDO[] toRemove)
  {
    var data = Undo.Clone(toRemove);
    foreach (var zdo in toRemove) Helper.RemoveZDO(zdo);
    return data;
  }

  public static ZDO[] Clone(IEnumerable<ZDO> data) => data.Select(zdo => zdo.Clone()).ToArray();
}
public class UndoRemove : MonoBehaviour, UndoAction
{

  private ZDO[] Data;
  public UndoRemove(IEnumerable<ZDO> data)
  {
    Data = global::InfinityHammer.Undo.Clone(data);
  }
  public void Undo()
  {
    Data = global::InfinityHammer.Undo.Place(Data);
  }

  public void Redo()
  {
    Data = global::InfinityHammer.Undo.Remove(Data);
  }

  public string UndoMessage() => $"Undo: Restored {(global::InfinityHammer.Undo.Print(Data))}";

  public string RedoMessage() => $"Redo: Removed {(global::InfinityHammer.Undo.Print(Data))}";
}

public interface UndoAction
{
  void Undo();
  void Redo();
  string UndoMessage();
  string RedoMessage();
}

public class UndoPlace : MonoBehaviour, UndoAction
{

  private ZDO[] Data;
  public UndoPlace(IEnumerable<ZDO> data)
  {
    Data = global::InfinityHammer.Undo.Clone(data);
  }
  public void Undo()
  {
    Data = global::InfinityHammer.Undo.Remove(Data);
  }

  public string UndoMessage() => $"Undo: Removed {(global::InfinityHammer.Undo.Print(Data))}";

  public void Redo()
  {
    Data = global::InfinityHammer.Undo.Place(Data);
  }
  public string RedoMessage() => $"Redo: Restored {(global::InfinityHammer.Undo.Print(Data))}";
}
