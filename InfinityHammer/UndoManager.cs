using System.Collections.Generic;
using System.Linq;

namespace Service {
  public interface UndoAction {
    void Undo();
    void Redo();
  }
  public class UndoManager {
    private static List<UndoAction> History = new List<UndoAction>();
    private static int Index = -1;
    private static bool Executing = false;
    public static void Add(UndoAction action) {
      // During undo/redo more steps won't be added.
      if (Executing) return;
      History = History.Take(Index + 1).ToList();
      History.Add(action);
      Index = History.Count - 1;
    }

    public static void Undo() {
      if (Index < 0) return;
      Executing = true;
      History[Index].Undo();
      Index--;
      Executing = false;
    }
    public static void Redo() {
      if (Index < History.Count - 1) {
        Executing = true;
        Index++;
        History[Index].Redo();
        Executing = false;
      }
    }
  }
}