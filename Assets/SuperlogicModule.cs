using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    public KMSelectable[] Buttons;
    public KMSelectable SubmitButton;
    public GameObject Container;
    public Material[] Greens;
    public Material[] Reds;
    public GameObject TmplText;
    public GameObject[] TmplButtons;
    public Mesh ButtonTrue;
    public Mesh ButtonFalse;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private List<GameObject>[] _unselectableButtons;
    private int _solution;
    private bool[] _selected;
    private bool _isSolved;

    // Advance widths of each character
    private static Dictionary<char, float> _advance = new Dictionary<char, float>
    {
        { '\0', .065f },    // advance width of a lettered button
        { '=', 0.05f },
        { '(', 0.025f },
        { ')', 0.025f },
        { '∧', 0.055f },
        { '∨', 0.055f },
        { '⊻', 0.05f },
        { '→', 0.061f },
        { '←', 0.06f },
        { '↔', 0.06f },
        { '|', 0.02f },
        { '↓', 0.035f },
        { '¬', 0.05f }
    };

    const int numVariables = 4;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        _selected = new bool[numVariables];
        _unselectableButtons = new List<GameObject>[numVariables];
        for (int i = 0; i < numVariables; i++)
        {
            _selected[i] = Rnd.Range(0, 2) == 0;
            _unselectableButtons[i] = new List<GameObject>();
        }

        tryAgain:
        var expressions = new List<Expression>();
        for (int var = 0; var < numVariables; var++)
            expressions.Add(generateRandomExpression(Enumerable.Range(0, numVariables).Where(v => v != var)));

        var solutions = new List<int>();
        for (int vals = 0; vals < 1 << numVariables; vals++)
        {
            for (int var = 0; var < numVariables; var++)
                if (((vals & (1 << var)) != 0) != expressions[var].Evaluate(vals))
                    goto invalid;
            solutions.Add(vals);
            invalid:;
        }

        if (solutions.Count != 1)
            goto tryAgain;
        _solution = solutions[0];

        Debug.LogFormat("[Superlogic #{0}] Equations:", _moduleId);
        for (int v = 0; v < numVariables; v++)
        {
            const float yAdvance = -0.07f;
            Debug.LogFormat("[Superlogic #{0}] {1} = {2}", _moduleId, (char) ('A' + v), expressions[v]);
            expressions[v].Instantiate(0.115f, false, (ch, x) =>
            {
                var isTxt = _advance.ContainsKey(ch);
                var template = isTxt ? TmplText : TmplButtons[0];
                var obj = Instantiate(template);
                obj.transform.parent = template.transform.parent;
                obj.transform.localScale = template.transform.localScale;
                obj.transform.localEulerAngles = new Vector3(isTxt ? 90 : 0, 0, 0);
                obj.transform.localPosition = new Vector3(x, 0, v * yAdvance + (isTxt ? .0075f : 0));
                if (isTxt)
                    obj.GetComponent<TextMesh>().text = ch.ToString();
                else
                    _unselectableButtons[ch - 'A'].Add(obj);
                return isTxt ? _advance[ch] : _advance['\0'];
            });
            Buttons[v].OnInteract = MakeButtonHandler(v);
        }

        Debug.LogFormat("[Superlogic #{0}] Solution:", _moduleId);
        for (int v = 0; v < numVariables; v++)
            Debug.LogFormat("[Superlogic #{0}] {1} = {2}", _moduleId, (char) ('A' + v), (_solution & (1 << v)) != 0);

        SubmitButton.OnInteract += SubmitButtonHandler;

        foreach (var btn in TmplButtons)
            Destroy(btn);
        SetTextures();
    }

    private bool SubmitButtonHandler()
    {
        Debug.LogFormat("[Superlogic #{0}] Submitted:", _moduleId);
        var allCorrect = true;
        for (int v = 0; v < _selected.Length; v++)
        {
            var correct = _selected[v] == ((_solution & (1 << v)) != 0);
            Debug.LogFormat("[Superlogic #{0}] {1} = {2} ({3})", _moduleId, (char) ('A' + v), _selected[v], correct ? "correct" : "wrong");
            allCorrect &= correct;
        }
        if (allCorrect)
        {
            Debug.LogFormat("[Superlogic #{0}] Module solved.", _moduleId);
            Module.HandlePass();
            _isSolved = true;
            SetTextures();
        }
        else
        {
            Debug.LogFormat("[Superlogic #{0}] Strike.", _moduleId);
            Module.HandleStrike();
        }
        return false;
    }

    private KMSelectable.OnInteractHandler MakeButtonHandler(int v)
    {
        return delegate
        {
            if (!_isSolved)
            {
                _selected[v] = !_selected[v];
                SetTextures();
            }
            return false;
        };
    }

    private void SetTextures()
    {
        for (int var = 0; var < _unselectableButtons.Length; var++)
        {
            for (int i = 0; i < _unselectableButtons[var].Count; i++)
            {
                _unselectableButtons[var][i].GetComponent<MeshFilter>().mesh = _selected[var] ? ButtonTrue : ButtonFalse;
                _unselectableButtons[var][i].GetComponent<MeshRenderer>().material = (_selected[var] ? Greens : Reds)[var + 1];
            }
            Buttons[var].GetComponent<MeshFilter>().mesh = _selected[var] ? ButtonTrue : ButtonFalse;
            Buttons[var].GetComponent<MeshRenderer>().material = (_selected[var] ? Greens : Reds)[var + 1];
        }
        SubmitButton.GetComponent<MeshFilter>().mesh = _isSolved ? ButtonTrue : ButtonFalse;
        SubmitButton.GetComponent<MeshRenderer>().material = (_isSolved ? Greens : Reds)[0];
    }

    private Expression generateRandomExpression(IEnumerable<int> variables)
    {
        var expressions = variables.Select(x => (Expression) new VariableExpression(x)).ToList().Shuffle();
        if (Rnd.Range(0, 10) == 0)
            expressions.RemoveAt(0);

        var notted = false;
        while (expressions.Count > 1)
        {
            var l = expressions.PickRandomAndRemove();
            var r = expressions.PickRandomAndRemove();
            if (!notted && Rnd.Range(0, 3) == 0)
            {
                l = new NotExpression(l);
                notted = true;
            }
            if (!notted && Rnd.Range(0, 3) == 0)
            {
                r = new NotExpression(r);
                notted = true;
            }
            expressions.Add(new BinaryOperatorExpression(l, r, (BinaryOperator) Rnd.Range(0, (int) BinaryOperator.NumOperators)));
        }
        return !notted && Rnd.Range(0, 3) == 0 ? new NotExpression(expressions[0]) : expressions[0];
    }

#pragma warning disable 414
    private string TwitchHelpMessage = string.Format(
        @"“!{{0}} A” to toggle a button, “!{{0}} submit” to press the submit button, or “!{{0}} submit {1}” to submit a full answer (must specify exactly four values; t=true and f=false).",
        string.Join(" ", Enumerable.Range(0, numVariables).Select(i => Rnd.Range(0, 2) == 0 ? "t" : "f").ToArray()));
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command.Length == 1 && command[0] >= 'a' && command[0] <= ('a' + numVariables - 1))
            return new[] { Buttons[command[0] - 'a'] };

        if (command == "s" || command == "submit")
            return new[] { SubmitButton };

        var m = Regex.Match(command, string.Format(@"^\s*s(ubmit)?(\s+[tf]){0}{1}{2}\s*$", "{", numVariables, "}"), RegexOptions.IgnoreCase);
        if (!m.Success)
            return null;

        var values = m.Groups[2].Value.Where(ch => "tfTF".Contains(ch)).Select(v => v == 't' || v == 'T').ToArray();
        if (values.Length != numVariables)
            return null;
        return Enumerable.Range(0, numVariables).Where(ix => values[ix] != _selected[ix]).Select(ix => Buttons[ix]).Concat(new[] { SubmitButton }).ToArray();
    }
}
