using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace InfinityHammer;

public class RulerParameters {
  public float? Radius;
  public float? Width;
  public float? Depth;
  public float? Height;
  public bool RotateWithPlayer;
}
public class Ruler {
  private static GameObject? Projector = null;
  private static CircleProjector? BaseProjector = null;

  private static CircleProjector GetBaseProjector() {
    var workbench = ZNetScene.instance.GetPrefab("piece_workbench");
    if (!workbench) throw new InvalidOperationException("Error: Unable to find the workbench object.");
    BaseProjector = workbench.GetComponentInChildren<CircleProjector>();
    return BaseProjector;
  }
  private static bool RotateWithPlayer = false;
  private static bool UseHeight = false;
  public static void Update() {
    if (Projector == null || !Player.m_localPlayer) return;
    var ghost = Player.m_localPlayer.m_placementGhost;
    Projector.SetActive(ghost);
    if (!ghost) return;
    var scale = Scaling.Command;
    Projector.transform.position = ghost.transform.position;
    if (RotateWithPlayer)
      Projector.transform.rotation = Player.m_localPlayer.transform.rotation;
    else
      Projector.transform.rotation = Quaternion.Euler(0f, ghost.transform.rotation.eulerAngles.y, 0f);

    if (Projector.GetComponent<CircleProjector>() is { } circle) {
      circle.m_radius = scale.X;
      circle.m_nrOfSegments = Math.Max(3, (int)(circle.m_radius * 4));
    }
    if (Projector.GetComponent<RectangleProjector>() is { } rect) {
      rect.m_width = scale.X;
      rect.m_depth = scale.Z;
      rect.m_nrOfSegments = Math.Max(3, (int)((rect.m_depth + rect.m_width) * 2));
    }
  }
  public static float Height => UseHeight ? Scaling.Command.Y : 0f;

  private static string DescriptionScale(GameObject projector) {
    var scale = Scaling.Command;
    var height = UseHeight ? $", h: {Format(scale.Y)}" : "";
    if (projector.GetComponent<RectangleProjector>()) {
      return $"w: {Format(scale.X)}, d: {Format(scale.Z)}" + height;
    }
    if (projector.GetComponent<CircleProjector>()) {
      return $"r: {Format(scale.X)}" + height;
    }
    return "";
  }
  private static string DescriptionPosition(GameObject projector) {
    var pos = projector.transform.position;
    return $"x: {Format(pos.x)}, z: {Format(pos.z)}, y: {Format(pos.y)}";
  }
  public static string Description() {
    if (Projector == null) return "";
    if (Hud.instance.m_pieceSelectionWindow.activeSelf) return "";
    var lines = new[] { DescriptionScale(Projector), DescriptionPosition(Projector) };
    return string.Join("\n", lines.Where(s => s != ""));
  }
  private static string Format(float f) => f.ToString("F1", CultureInfo.InvariantCulture);

  private static GameObject InitializeGameObject(RulerParameters pars) {
    Projector = new();
    Projector.layer = LayerMask.NameToLayer("character_trigger");
    return Projector;
  }
  public static void InitializeProjector(RulerParameters pars, GameObject obj) {
    if (BaseProjector == null)
      BaseProjector = GetBaseProjector();
    RotateWithPlayer = pars.RotateWithPlayer;
    var scale = Scaling.Command;
    if (pars.Radius.HasValue) {
      var circle = obj.AddComponent<CircleProjector>();
      circle.m_prefab = BaseProjector.m_prefab;
      circle.m_mask = BaseProjector.m_mask;
      circle.m_nrOfSegments = 3;
      scale.SetScaleX(pars.Radius.Value);
    }
    if (pars.Depth.HasValue && pars.Width.HasValue) {
      var rect = obj.AddComponent<RectangleProjector>();
      rect.m_prefab = BaseProjector.m_prefab;
      rect.m_mask = BaseProjector.m_mask;
      rect.m_nrOfSegments = 3;
      scale.SetScaleX(pars.Width.Value);
      scale.SetScaleZ(pars.Depth.Value);
    }
    UseHeight = pars.Height.HasValue;
    if (pars.Height.HasValue) {
      scale.SetScaleY(pars.Height.Value);
    }
  }
  public static void Create(RulerParameters pars) {
    Remove();
    if (pars.Radius == null && pars.Width == null && pars.Depth == null) return;
    var obj = InitializeGameObject(pars);
    InitializeProjector(pars, InitializeGameObject(pars));
  }

  public static void Remove() {
    if (Projector != null)
      UnityEngine.Object.Destroy(Projector);
    Projector = null;
  }
}
