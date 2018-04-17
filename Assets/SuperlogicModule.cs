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
    public GameObject[] TmplTexts;
    public GameObject[] TmplButtons;
    public Mesh ButtonTrue;
    public Mesh ButtonFalse;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private int _numVariables;
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

    void Awake()
    {
        _numVariables = Rnd.Range(3, 5);
        TwitchHelpMessage = string.Format(
            @"“!{{0}} {1}” to toggle a button, “!{{0}} submit” to press the submit button, or “!{{0}} submit {0}” to submit a full answer (must specify exactly {2} values; t=true and f=false).",
            string.Join(" ", Enumerable.Range(0, _numVariables).Select(i => Rnd.Range(0, 2) == 0 ? "t" : "f").ToArray()), (char) ('A' + Rnd.Range(0, _numVariables)), _numVariables);
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = _numVariables; i < 4; i++)
        {
            Buttons[i].gameObject.SetActive(false);
            TmplTexts[i].SetActive(false);
        }

        _selected = new bool[_numVariables];
        _unselectableButtons = new List<GameObject>[_numVariables];
        for (int i = 0; i < _numVariables; i++)
        {
            _selected[i] = Rnd.Range(0, 2) == 0;
            _unselectableButtons[i] = new List<GameObject>();
        }

        tryAgain:
        var expressions = new List<Expression>();
        for (int var = 0; var < _numVariables; var++)
            expressions.Add(generateRandomExpression(Enumerable.Range(0, _numVariables).Where(v => v != var)));

        var solutions = new List<int>();
        for (int vals = 0; vals < 1 << _numVariables; vals++)
        {
            for (int var = 0; var < _numVariables; var++)
                if (((vals & (1 << var)) != 0) != expressions[var].Evaluate(vals))
                    goto invalid;
            solutions.Add(vals);
            invalid:;
        }

        if (solutions.Count != 1)
            goto tryAgain;
        _solution = solutions[0];

        Debug.LogFormat("[Superlogic #{0}] Equations:", _moduleId);
        var maxAdvance = 0f;
        for (int v = 0; v < _numVariables; v++)
        {
            const float yAdvance = -0.07f;
            Debug.LogFormat("[Superlogic #{0}] {1} = {2}", _moduleId, (char) ('A' + v), expressions[v]);
            maxAdvance = Mathf.Max(maxAdvance, expressions[v].Instantiate(0.115f, false, (ch, x) =>
            {
                var isTxt = _advance.ContainsKey(ch);
                var template = isTxt ? TmplTexts[0] : TmplButtons[0];
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
            }));
            Buttons[v].OnInteract = MakeButtonHandler(v);
        }
        maxAdvance += _advance['='] + _advance['\0'];

        Debug.LogFormat("[Superlogic #{0}] Solution:", _moduleId);
        for (int v = 0; v < _numVariables; v++)
            Debug.LogFormat("[Superlogic #{0}] {1} = {2}", _moduleId, (char) ('A' + v), (_solution & (1 << v)) != 0);

        SubmitButton.OnInteract += SubmitButtonHandler;

        foreach (var btn in TmplButtons)
            Destroy(btn);
        SetTextures();

        var scale = Mathf.Min((_numVariables == 3 ? .5f : .47f), (_numVariables == 3 ? .15f : .1625f) / maxAdvance);
        Container.transform.localScale = new Vector3(scale, scale, scale);
        Container.transform.localPosition = new Vector3(_numVariables == 3 ? -.075f : -.08f, .01501f, _numVariables == 3 ? .02f : .03f);
    }

    private bool SubmitButtonHandler()
    {
        SubmitButton.AddInteractionPunch();
        if (_isSolved)
            return false;

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
            Audio.PlaySoundAtTransform("Plipplop", SubmitButton.transform);
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
            Buttons[v].AddInteractionPunch();
            if (!_isSolved)
            {
                Audio.PlaySoundAtTransform(_selected[v] ? "Plop" : "Plip", Buttons[v].transform);
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
        while (expressions.Count > 2)
            expressions.RemoveAt(0);

        var notted = 0;
        const int maxNotted = 1;
        while (expressions.Count > 1)
        {
            var l = expressions.PickRandomAndRemove();
            var r = expressions.PickRandomAndRemove();
            if (notted < maxNotted && Rnd.Range(0, 3) == 0)
            {
                l = new NotExpression(l);
                notted++;
            }
            if (notted < maxNotted && Rnd.Range(0, 3) == 0)
            {
                r = new NotExpression(r);
                notted++;
            }
            expressions.Add(new BinaryOperatorExpression(l, r, (BinaryOperator) Rnd.Range(0, (int) BinaryOperator.NumOperators)));
        }
        return notted < maxNotted && Rnd.Range(0, 3) == 0 ? new NotExpression(expressions[0]) : expressions[0];
    }

#pragma warning disable 414
    private string TwitchHelpMessage;
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command.Length == 1 && command[0] >= 'a' && command[0] <= ('a' + _numVariables - 1))
            return new[] { Buttons[command[0] - 'a'] };

        if (command == "s" || command == "submit")
            return new[] { SubmitButton };

        var m = Regex.Match(command, string.Format(@"^\s*s(ubmit)?(\s+[tf]){0}{1}{2}\s*$", "{", _numVariables, "}"), RegexOptions.IgnoreCase);
        if (!m.Success)
            return null;

        var values = m.Groups[2].Value.Where(ch => "tfTF".Contains(ch)).Select(v => v == 't' || v == 'T').ToArray();
        if (values.Length != _numVariables)
            return null;
        return Enumerable.Range(0, _numVariables).Where(ix => values[ix] != _selected[ix]).Select(ix => Buttons[ix]).Concat(new[] { SubmitButton }).ToArray();
    }
}
