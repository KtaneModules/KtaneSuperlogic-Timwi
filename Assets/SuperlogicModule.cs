using System.Collections.Generic;
using System.Linq;
using Superlogic;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Superlogic
/// Created by Timwi
/// </summary>
public class SuperlogicModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var numVariables = 3;
        var variables = Enumerable.Range(0, numVariables).Select(x => (Expression) new VariableExpression(x)).ToList().Shuffle();
        if (Rnd.Range(0, 10) == 0)
            variables.RemoveAt(0);

        while (variables.Count > 1)
        {
            var l = pickRandomAndRemove(variables);
            var r = pickRandomAndRemove(variables);
            variables.Add(new BinaryOperatorExpression(l, r, (BinaryOperator) Rnd.Range(0, (int) BinaryOperator.NumOperators)));
        }
    }

    private T pickRandomAndRemove<T>(List<T> list)
    {
        var ix = Rnd.Range(0, list.Count);
        var t = list[ix];
        list.RemoveAt(ix);
        return t;
    }
}
