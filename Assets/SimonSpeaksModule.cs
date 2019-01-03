using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using SimonSpeaks;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Simon Speaks
/// Created by Timwi
/// </summary>
public class SimonSpeaksModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public KMSelectable[] Bubbles;
    public TextMesh[] BubbleTexts;
    public MeshRenderer[] BubbleBodies;
    public MeshRenderer[] BubbleOutlines;
    public MeshFilter[] BubbleBodiesF;
    public MeshFilter[] BubbleOutlinesF;
    public Mesh[] BubbleBodyMeshes;
    public Mesh[] BubbleOutlineMeshes;

    private Vector3[] _bubbleTextPositions;
    private Quaternion[] _bubbleTextRotations;

    public static readonly Color[] _bodyColors = @"1C1C1C,4D73FF,3BD842,49D8EC,F44949,BA6DFF,FFFA42,FFFFFF,A0A0A0"
        .Split(',').Select(str => Enumerable.Range(0, 3).Select(i => Convert.ToInt32(str.Substring(2 * i, 2), 16) / 255f).ToArray()).Select(arr => new Color(arr[0], arr[1], arr[2])).ToArray();
    public static readonly Color[] _outlineColors = @"AEAEAE,ABBDFF,2F7C30,318894,9F2A2A,7C40B2,6D6B30,828282,727272"
        .Split(',').Select(str => Enumerable.Range(0, 3).Select(i => Convert.ToInt32(str.Substring(2 * i, 2), 16) / 255f).ToArray()).Select(arr => new Color(arr[0], arr[1], arr[2])).ToArray();
    public static readonly bool[] _textIsWhite = new[] { true, true, false, false, false, false, false, false, false };

    private static readonly string[][] _wordsTable = new[] {
        new[] { "black", "sort", "zwart", "nigra", "musta", "noir", "schwarz", "fekete", "nero" },
        new[] { "blue", "blå", "blauw", "blua", "sininen", "bleu", "blau", "kék", "blu" },
        new[] { "green", "grøn", "groen", "verda", "vihreä", "vert", "grün", "zöld", "verde" },
        new[] { "cyan", "turkis", "turkoois", "turkisa", "turkoosi", "turquoise", "türkis", "türkiz", "turchese" },
        new[] { "red", "rød", "rood", "ruĝa", "punainen", "rouge", "rot", "piros", "rosso" },
        new[] { "purple", "lilla", "purper", "purpura", "purppura", "pourpre", "lila", "bíbor", "porpora" },
        new[] { "yellow", "gul", "geel", "flava", "keltainen", "jaune", "gelb", "sárga", "giallo" },
        new[] { "white", "hvid", "wit", "blanka", "valkoinen", "blanc", "weiß", "fehér", "bianco" },
        new[] { "gray", "grå", "grijs", "griza", "harmaa", "gris", "grau", "szürke", "grigio" }
    };

    private static readonly string[] _languageNames = { "English", "Danish", "Dutch", "Esperanto", "Finnish", "French", "German", "Hungarian", "Italian" };
    private static readonly string[] _positionNames = { "top-left", "top-middle", "top-right", "middle-left", "middle-center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int[] _colors;
    private int[] _words;
    private int[] _languages;
    private int[] _shapes;
    private int[] _sounds;
    private int[] _sequence;
    private int[] _solution;
    private bool _makeSounds;
    private Coroutine _blinker;
    private int _stage;
    private int _subprogress;
    private bool _isSolved;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _stage = 0;
        _subprogress = 0;
        _isSolved = false;

        _bubbleTextPositions = BubbleTexts.Select(b => b.transform.localPosition).ToArray();
        _bubbleTextRotations = BubbleTexts.Select(b => b.transform.localRotation).ToArray();

        _colors = Enumerable.Range(0, 9).ToArray().Shuffle();
        _words = Enumerable.Range(0, 9).ToArray().Shuffle();
        _languages = Enumerable.Range(0, 9).ToArray().Shuffle();
        _shapes = Enumerable.Range(0, 9).ToArray().Shuffle();
        _sounds = Enumerable.Range(0, 9).ToArray().Shuffle();

        for (int i = 0; i < 9; i++)
        {
            BubbleBodies[i].material.color = _bodyColors[_colors[i]];
            BubbleOutlines[i].material.color = _outlineColors[_colors[i]];
            BubbleTexts[i].color = _textIsWhite[_colors[i]] ? Color.white : Color.black;
            BubbleTexts[i].text = _wordsTable[_words[i]][_languages[i]];
            BubbleTexts[i].transform.localPosition = _bubbleTextPositions[_shapes[i]];
            BubbleTexts[i].transform.localRotation = _bubbleTextRotations[_shapes[i]];
            BubbleBodiesF[i].sharedMesh = BubbleBodyMeshes[_shapes[i]];
            BubbleOutlinesF[i].sharedMesh = BubbleOutlineMeshes[_shapes[i]];
            Bubbles[i].transform.localEulerAngles = new Vector3(0, i == 2 && _shapes[i] == 4 ? 12f : Rnd.Range(-10f, 10f), 0);
            Bubbles[i].OnInteract = bubblePressed(i);
        }

        _sequence = Enumerable.Range(0, 5).Select(i => Rnd.Range(0, 9)).ToArray();
        _solution = new int[5];
        _solution[0] = (_sequence[0] + snValue(0)) % 9;
        _solution[1] = Array.IndexOf(_shapes, (_shapes[_sequence[1]] + snValue(1)) % 9);
        _solution[2] = Array.IndexOf(_languages, (_languages[_sequence[2]] + snValue(2)) % 9);
        _solution[3] = Array.IndexOf(_colors, (_words[_sequence[3]] + snValue(3)) % 9);
        _solution[4] = Array.IndexOf(_words, (_colors[_sequence[4]] + snValue(4)) % 9);

        Debug.LogFormat("[Simon Speaks #{0}] Bubble shapes are: {1}", _moduleId, _shapes.Join(", "));
        Debug.LogFormat("[Simon Speaks #{0}] Bubble colors are: {1}", _moduleId, _colors.Select(c => _wordsTable[c][0]).Join(", "));
        Debug.LogFormat("[Simon Speaks #{0}] Words are: {1}", _moduleId, _words.Select(w => _wordsTable[w][0]).Join(", "));
        Debug.LogFormat("[Simon Speaks #{0}] Languages are: {1}", _moduleId, _languages.Select(lng => _languageNames[lng]).Join(", "));
        Debug.LogFormat("[Simon Speaks #{0}] Flashing sequence is: {1}", _moduleId, _sequence.Select(i => _positionNames[i]).Join(", "));
        Debug.LogFormat("[Simon Speaks #{0}] Solution is: {1}", _moduleId, _solution.Select(i => _positionNames[i]).Join(", "));

        startBlinker(0);
    }

    private int snValue(int ix)
    {
        var ch = Bomb.GetSerialNumber()[ix];
        return ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1;
    }

    private KMSelectable.OnInteractHandler bubblePressed(int ix)
    {
        return delegate
        {
            Audio.PlaySoundAtTransform("Bell" + (_sounds[ix] + 1), Bubbles[ix].transform);
            Bubbles[ix].AddInteractionPunch(.5f);
            if (_isSolved)
                return false;

            _makeSounds = true;
            showFlashed(null);

            if (ix != _solution[_subprogress])
            {
                Debug.LogFormat(@"[Simon Speaks #{0}] Expected {1}; you pressed {2}. Strike.", _moduleId, _positionNames[_solution[_subprogress]], _positionNames[ix]);
                Module.HandleStrike();
                _subprogress = 0;
                startBlinker(2.1f);
            }
            else
            {
                if (_subprogress == _stage)
                {
                    _stage++;
                    _subprogress = 0;
                    if (_stage == 5)
                    {
                        if (_blinker != null)
                            StopCoroutine(_blinker);
                        Debug.LogFormat("[Simon Speaks #{0}] Pressing {1} was correct. Module solved.", _moduleId, _positionNames[ix]);
                        Module.HandlePass();
                        StartCoroutine(victory());
                        _isSolved = true;
                        return false;
                    }
                    startBlinker(2.1f);
                }
                else
                {
                    _subprogress++;
                    startBlinker(4.7f);
                }

                Debug.LogFormat("[Simon Speaks #{0}] Pressing {1} was correct; now at stage {2} key {3}.", _moduleId, _positionNames[ix], _stage + 1, _subprogress + 1);
            }

            return false;
        };
    }

    private IEnumerator victory()
    {
        yield return new WaitForSeconds(.5f);
        var melody = new[] { 9, 8, 6, 5, 7, 6, 4, 2, 3 };
        for (int mIx = 0; mIx < melody.Length; mIx++)
        {
            Audio.PlaySoundAtTransform("Bell" + melody[mIx], Bubbles[melody[mIx] - 1].transform);
            for (int i = 0; i < 9; i++)
            {
                BubbleBodies[i].material.color = (i % 2 == mIx % 2 ? _outlineColors : _bodyColors)[_colors[i]];
                BubbleOutlines[i].material.color = (i % 2 == mIx % 2 ? _bodyColors : _outlineColors)[_colors[i]];
                BubbleTexts[i].color = (_textIsWhite[_colors[i]] ^ (i % 2 == mIx % 2)) ? Color.white : Color.black;
            }
            yield return new WaitForSeconds(.15f);
        }
        showFlashed(null);
    }

    private void startBlinker(float delay)
    {
        if (_blinker != null)
            StopCoroutine(_blinker);
        _blinker = StartCoroutine(runBlinker(delay));
    }

    private void startBlinker()
    {
        if (_blinker != null)
            StopCoroutine(_blinker);
        _blinker = StartCoroutine(runBlinker());
    }

    private IEnumerator runBlinker(float delay = 0)
    {
        yield return new WaitForSeconds(delay);

        if (_subprogress != 0)
        {
            Debug.LogFormat("[Simon Speaks #{0}] Waited too long; input reset. Now at stage {1} key 1.", _moduleId, _stage + 1);
            _subprogress = 0;
        }
        while (!_isSolved)
        {
            for (int i = 0; i <= _stage; i++)
            {
                if (_makeSounds)
                    Audio.PlaySoundAtTransform("Bell" + (_sounds[_sequence[i]] + 1), Bubbles[_sequence[i]].transform);
                showFlashed(_sequence[i]);
                yield return new WaitForSeconds(.4f);
                showFlashed(null);
                yield return new WaitForSeconds(.3f);
            }
            yield return new WaitForSeconds(1.6f);
        }
    }

    private void showFlashed(int? ix)
    {
        for (int i = 0; i < 9; i++)
        {
            BubbleBodies[i].material.color = (i == ix ? _outlineColors : _bodyColors)[_colors[i]];
            BubbleOutlines[i].material.color = (i == ix ? _bodyColors : _outlineColors)[_colors[i]];
            BubbleTexts[i].color = (_textIsWhite[_colors[i]] ^ (i == ix)) ? Color.white : Color.black;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} TL, TM [top-left, top-middle, etc.]";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        var split = command.Trim().ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 0)
            return null;
        var skip = split[0].Equals("press", StringComparison.InvariantCulture | StringComparison.CurrentCulture) ? 1 : 0;
        if (!split.Skip(skip).Any())
            return null;

        var btns = new List<KMSelectable>();
        foreach (var cmd in split.Skip(skip))
            switch (cmd.Replace("center", "middle").Replace("centre", "middle"))
            {
                case "tl": case "lt": case "topleft": case "lefttop": btns.Add(Bubbles[0]); break;
                case "tm": case "tc": case "mt": case "ct": case "topmiddle": case "middletop": btns.Add(Bubbles[1]); break;
                case "tr": case "rt": case "topright": case "righttop": btns.Add(Bubbles[2]); break;

                case "ml": case "cl": case "lm": case "lc": case "middleleft": case "leftmiddle": btns.Add(Bubbles[3]); break;
                case "mm": case "cm": case "mc": case "cc": case "middle": case "middlemiddle": btns.Add(Bubbles[4]); break;
                case "mr": case "cr": case "rm": case "rc": case "middleright": case "rightmiddle": btns.Add(Bubbles[5]); break;

                case "bl": case "lb": case "bottomleft": case "leftbottom": btns.Add(Bubbles[6]); break;
                case "bm": case "bc": case "mb": case "cb": case "bottommiddle": case "middlebottom": btns.Add(Bubbles[7]); break;
                case "br": case "rb": case "bottomright": case "rightbottom": btns.Add(Bubbles[8]); break;

                default: return null;
            }
        return btns.ToArray();
    }
}
