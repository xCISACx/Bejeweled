using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Board))]
public class GemArrayWrapperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Board board = (Board)target;

        if (GUILayout.Button("Initialize Gems Array"))
        {
            board.InitializeGemsArray();
        }

        if (board.GemArrayWrapper != null && board.GemArrayWrapper.Gems != null)
        {
            for (int i = 0; i < board.Width; i++)
            {
                for (int j = 0; j < board.Height; j++)
                {
                    EditorGUI.BeginChangeCheck();
                    board.GemArrayWrapper.Gems[i, j] = (Gem)EditorGUILayout.ObjectField(
                        $"Gems[{i}, {j}]",
                        board.GemArrayWrapper.Gems[i, j],
                        typeof(Gem),
                        true
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        // Do something when the Gem is changed in the inspector
                    }
                }
            }
        }
    }
}
