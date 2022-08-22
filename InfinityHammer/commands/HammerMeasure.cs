using System;
using System.Linq;
using UnityEngine;

namespace InfinityHammer;
public class HammerMeasureCommand {
  private static int[] IgnoredLayers = new[] { LayerMask.NameToLayer("character_trigger"), LayerMask.NameToLayer("viewblock"), LayerMask.NameToLayer("pathblocker") };
  public static void CheckWithCollider(GameObject obj) {
    if (obj.transform.rotation != Quaternion.identity) return;
    var name = Utils.GetPrefabName(obj);
    if (Configuration.Dimensions.ContainsKey(name.ToLower())) return;
    if (!ZNetScene.instance.GetPrefab(name)) return;
    var colliders = obj.GetComponentsInChildren<Collider>().Where(c => !IgnoredLayers.Contains(c.gameObject.layer)).ToArray();
    if (colliders.Length == 0) return;
    var bounds = colliders[0].bounds;
    foreach (var c in colliders)
      bounds.Encapsulate(c.bounds);
    Configuration.SetDimension(name, bounds.size);
  }
  public HammerMeasureCommand() {
    CommandWrapper.RegisterEmpty("hammer_measure");
    Helper.Command("hammer_measure", "Tries to measure all structures.", (args) => {
      Helper.CheatCheck();
      ZNetView.m_forceDisableInit = true;
      foreach (var prefab in ZNetScene.instance.m_prefabs) {
        if (prefab.name == "Player") continue;
        if (prefab.name.StartsWith("_", StringComparison.Ordinal)) continue;
        if (prefab.name.StartsWith("fx_", StringComparison.Ordinal)) continue;
        if (prefab.name.StartsWith("sfx_", StringComparison.Ordinal)) continue;
        if (!prefab.GetComponentInChildren<Collider>()) continue;
        var obj = UnityEngine.Object.Instantiate(prefab);
        CheckWithCollider(obj);
        UnityEngine.Object.Destroy(obj);
      }
      ZNetView.m_forceDisableInit = false;
      Helper.AddMessage(args.Context, "Objects measured.");
    });
  }
}
