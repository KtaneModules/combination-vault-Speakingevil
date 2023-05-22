using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class VaultScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public Transform door;
    public Transform[] pins;
    public Transform[] dialrots;
    public List<KMSelectable> dials;
    public KMSelectable submit;
    public KMSelectable reset;
    public Renderer[] drends;
    public Material[] dialmats;
    public Renderer[] backings;
    public Material[] bmats;
    public TextMesh[] displays;
    public bool cursed;

    private readonly string[] dirarrows = new string[4] { "\u25b2", "\u25ba", "\u25bc", "\u25c4"};
    private readonly string thirtysix = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int gridnum;
    private string[] grid;
    private int[][] targets = new int[3][] { new int[4], new int[4], new int[4]};
    private int[][] tesseract = new int[256][];
    private int pos;
    private List<int> pastpos = new List<int> { };
    private int remaining = 36;
    private int unlock;
    private bool pressable = true;

    private static int moduleIDCounter;
    private static int cursedIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private List<int[]> LinDep(int[][] vec, List<int[]> vtot)
    {
        for (int i = 0; i < 4; i++)
        {
            int[] a = VProd(vec[0], i);
            for (int j = 0; j < 4; j++)
            {
                int[] b = VProd(vec[1], j);
                for (int k = 0; k < 4; k++)
                {
                    int[] c = VProd(vec[2], k);
                    int[] d = VSum(new int[][] { a, b, c });
                    if (vtot.Any(x => x.SequenceEqual(d)))
                        vtot.Remove(d);
                }
            }
        }
        return vtot;
    }

    private int[] VSum(int[][] x)
    {
        if (x.Length > 2)
        {
            int[] y = VSum(new int[][] { x[0], x[1] });
            int[][] z = x.Skip(2).ToArray();
            return VSum(new int[][] { y }.Concat(z).ToArray());
        }
        int[] s = new int[4];
        for (int i = 0; i < 4; i++)
            s[i] = (x[0][i] + x[1][i]) % 4;
        return s;
    }

    private int[] VProd(int[] x, int m)
    {
        int[] p = new int[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            p[i] = x[i] * m;
            p[i] %= 4;
        }
        return p;
    }

    private bool Dup(int[][] x)
    {
        for (int i = 0; i < x.Length - 1; i++)
            for (int j = i + 1; j < x.Length; j++)
                if (x[i].SequenceEqual(x[j]))
                    return true;
        return false;
    }

    private void Start()
    {
        moduleID = cursed ? ++cursedIDCounter : ++moduleIDCounter;
        gridnum = Random.Range(0, 24);
        int[] dassign = Grids.c[gridnum].Select((x, i) => "RGBY".IndexOf(x.ToString())).ToArray();
        for (int i = 0; i < 4; i++)
            drends[i].material = dialmats[dassign[i]];
        if (cursed)
        {
            Color[] cols = new Color[4] { new Color(1, 0, 0), new Color(0, 1, 0), new Color(0, 0.25f, 1), new Color(1, 0.875f, 0) };
            for (int i = 0; i < 16; i++)
                displays[i + 1].color = cols[dassign[i / 4]];
        }
        else
            for (int i = 0; i < 4; i++)
                backings[i].material = bmats[dassign[i]];
        grid = Grids.g[gridnum];
        string sn = info.GetSerialNumber();
        int[] scoords = Enumerable.Range(0, 3).Select(x => sn.Skip(x * 2).Take(2).ToArray()).Select(x => (thirtysix.IndexOf(x[0]) * 36 + thirtysix.IndexOf(x[1])) % 256).ToArray();
        for (int i = 1; i < 3; i++)
            if (scoords[i] == scoords[1 - 1])
            {
                scoords[i] += scoords[(i + 1) % 3];
                scoords[i] %= 256;
            }      
        for (int i = 0; i < 3; i++)
        {
            int[] c = new int[2] { scoords[i] / 16, scoords[i] % 16 };
            for (int j = 0; j < 4; j++)
                targets[i][j] = grid[c[0] + (j / 2)][c[1] + (j % 2)] - '0';
        }
        if (cursed)
        {
            int attempts = 0;
        restart:;
            int iter = 0;
            attempts++;
            int constdial = Random.Range(0, 4);
            int[] c = new int[4];
            tesseract[0] = new int[4];
            for (int i = 0; i < 4; i++)
                tesseract[0][i] = Random.Range(0, 4);
            for (int i = 1; i < 4; i++)
            {
                do
                {
                    if(constdial > 0 || i < 2)
                        for (int j = 0; j < 4; j++)
                            c[j] = j < 1 ? 1 : Random.Range(0, 4);
                    tesseract[i] = new int[4];
                    for (int k = 0; k < 4; k++)
                        tesseract[i][k] = (tesseract[i - 1][k] + c[k]) % 4;
                } while (Dup(tesseract.Take(i).ToArray()));
            }
            for (int i = 4; i < 16; i += 4)
            {
                do
                {
                    iter++;
                    if (iter > 64)
                        goto restart;
                    if (constdial != 1 || i < 5)
                        for (int j = 0; j < 4; j++)
                            c[j] = j == 1 ? 1 : Random.Range(0, 4);
                    for (int j = 0; j < 4; j++)
                    {
                        tesseract[i + j] = new int[4];
                        for (int k = 0; k < 4; k++)
                            tesseract[i + j][k] = (tesseract[i + j - 4][k] + c[k]) % 4;
                    }
                } while (Dup(tesseract.Take(4 + i).ToArray()));
            }
            for (int i = 16; i < 64; i += 16)
            {
                do
                {
                    iter++;
                    if (iter > 64)
                        goto restart;
                    if(constdial != 2 || i < 17)
                        for (int j = 0; j < 4; j++)
                            c[j] = j == 2 ? 1 : Random.Range(0, 4);
                    for (int j = 0; j < 16; j++)
                    {
                        tesseract[i + j] = new int[4];
                        for (int k = 0; k < 4; k++)
                            tesseract[i + j][k] = (tesseract[i + j - 16][k] + c[k]) % 4;
                    }
                } while (Dup(tesseract.Take(16 + i).ToArray()));
            }
            for (int i = 64; i < 256; i += 64)
            {
                do
                {
                    iter++;
                    if (iter > 64)
                        goto restart;
                    if(constdial < 3 || i < 65)
                        for (int j = 0; j < 4; j++)
                            c[j] = j > 2 ? 1 : Random.Range(0, 4);
                    for (int j = 0; j < 64; j++)
                    {
                        tesseract[i + j] = new int[4];
                        for (int k = 0; k < 4; k++)
                            tesseract[i + j][k] = (tesseract[i + j - 64][k] + c[k]) % 4;
                    }
                } while (Dup(tesseract.Take(64 + i).ToArray()));
            }
            Debug.Log("Pass (" + attempts + ")");
        }
        else
        {
            int[][] vec = new int[4][] { new int[4] { 1, -1, -1, -1}, new int[4] { -1, 1, -1, -1}, new int[4] { -1, -1, 1, -1}, new int[4] { -1, -1, -1, 1} };
        restart:;
            for (int i = 0; i < 4; i++)
            {
            retry:;
                bool[] v = new bool[4];
                for (int j = 0; j < 4; j++)
                    vec[i][j] = i == j ? 1 : -1;
                do
                {
                    for (int j = 0; j < 4; j++)
                        v[j] = Random.Range(0, 2) < 1;
                    v[i] = false;
                } while (vec.Take(i).Any(x => x.Select(y => y == 0).SequenceEqual(v)) || vec.Take(i).Any(x => x.Select(y => y != 0).SequenceEqual(v)));
                for (int j = 0; j < 4; j++)
                {
                    if (v[j])
                        vec[i][j] = 0;
                    if (vec[i][j] < 0)
                        vec[i][j] = new int[] { 1, 3 }[Random.Range(0, 2)];
                }
                int lim = new int[4] { 0, 4, 16, 64 }[i];
                for (int j = 0; j < lim; j++)
                    if (vec[i].SequenceEqual(VSum(new int[][] { VProd(vec[0], i % 4), VProd(vec[1], (i / 4) % 4), VProd(vec[2], (i / 16) % 4) })))
                        goto retry;
            }
            for (int i = 0; i < 256; i++)
                tesseract[i] = VSum(new int[][] { VProd(vec[0], i % 4), VProd(vec[1], (i / 4) % 4), VProd(vec[2], (i / 16) % 4), VProd(vec[3], i / 64) });
            if (tesseract.Count(x => x.Sum() < 1) > 1)
                goto restart;
            for (int i = 0; i < 4; i++)
                Debug.LogFormat("[Combination Vault #{0}] Turning the {1}-{2} dial clockwise turns: TL {3}°, TR {4}°, BL {5}°, BR {6}°", moduleID, i / 2 < 1 ? "top" : "bottom", i % 2 < 1 ? "left" : "right", new string[] { "0", "90", "180", "-90" }[vec[i][0]], new string[] { "0", "90", "180", "-90" }[vec[i][1]], new string[] { "0", "90", "180", "-90" }[vec[i][2]], new string[] { "0", "90", "180", "-90" }[vec[i][3]]);               
        }
        Debug.Log(string.Join(", ", tesseract.Select((x, i) => "(" + (i % 4).ToString() + ((i / 4) % 4).ToString() + ((i / 16) % 4).ToString() + (i / 64).ToString() + ")-(" + string.Join("", x.Select(y => y.ToString()).ToArray()) + ")").ToArray()));
        pos = Random.Range(0, 256);
        int[] p = new int[4];
        do {
            pos = Random.Range(0, 256);
            p = tesseract[pos];
        } while (targets.Any(x => x.SequenceEqual(p)));
        Debug.LogFormat("[{0} Vault #{1}] The initial configuration of the dials are: {2}", cursed ? "Cursed" : "Combination", moduleID, string.Join("", p.Select(x => dirarrows[x]).ToArray()));
        for(int i = 0; i < 4; i++)
        {
            dialrots[i].localEulerAngles = new Vector3(0, 90 * (p[i] + 2), 0);
            dialrots[i + 4].localEulerAngles = new Vector3(0, 90 * p[i], 0);
        }
        Debug.LogFormat("[{0} Vault #{1}] The colours of the dials are: {2}", cursed ? "Cursed" : "Combination", moduleID, Grids.c[gridnum]);
        Debug.LogFormat("[{0} Vault #{1}] The pairs of indices are: {2}", cursed ? "Cursed" : "Combination", moduleID, string.Join(", ", scoords.Select(x => thirtysix[x / 16].ToString() + thirtysix[x % 16].ToString()).ToArray()));
        Debug.LogFormat("[{0} Vault #{1}] The target configurations of the dials, in order, are: {2}", cursed ? "Cursed" : "Combination", moduleID, string.Join(", ", targets.Select(x => string.Join("", x.Select(y => dirarrows[y]).ToArray())).ToArray()));
        foreach(KMSelectable dial in dials)
        {
            int b = dials.IndexOf(dial);
            dial.OnInteract = delegate ()
            {
                if(!moduleSolved && remaining > 0 && pressable)
                {
                    Audio.PlaySoundAtTransform("Rotate", dial.transform);
                    remaining--;
                    displays[0].text = remaining.ToString();
                    int[] s = tesseract[pos];
                    pastpos.Add(pos);
                    int[] e = new int[4] { pos % 4, (pos / 4) % 4, (pos / 16) % 4, pos / 64};
                    e[b]++;
                    e[b] %= 4;
                    pos = 0;
                    for(int i = 0; i < 4; i++)
                    {
                        pos *= 4;
                        pos += e[3 - i];
                    }
                    StartCoroutine(Rotate(s, tesseract[pos], 0.6f, false));
                }
                return false;
            };
        }
        submit.OnInteract = delegate ()
        {
            if (!moduleSolved && pressable)
            {
                submit.AddInteractionPunch(0.5f);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
                if (tesseract[pos].SequenceEqual(targets[unlock]))
                {
                    Audio.PlaySoundAtTransform("Unlock", transform);
                    for (int i = 4 * unlock; i < 4 * (unlock + 1); i++)
                    {
                        float arg = Mathf.PI * int.Parse(pins[i].name) / 6f;
                        pins[i].localPosition = new Vector3(Mathf.Sin(arg) * 0.09f, 0, Mathf.Cos(arg) * 0.09f);
                    }
                    if (unlock > 1)
                        StartCoroutine(Unlock());
                    else
                    {
                        unlock++;
                        pastpos.Add(-1);
                    }
                }
                else
                    module.HandleStrike();
            }
            return false;
        };
        reset.OnInteract = delegate ()
        {
            if (pastpos.Count() > 0)
                StartCoroutine(Reset());
            return false;
        };
    }

    private IEnumerator Rotate(int[] start, int[] end, float duration, bool reset)
    {
        pressable = false;
        float[] rot = new float[4];
        for(int i = 0; i < 4; i++)
        {
            float x = Mathf.Abs(start[i] - end[i]);
            if (x % 2 == 1)
            {
                if (start[i] == 3 && end[i] == 0)
                    rot[i] = 90f;
                else if ((start[i] == 0 && end[i] == 3) || start[i] > end[i])
                    rot[i] = -90f;
                else
                    rot[i] = 90f;
            }
            else
                rot[i] = 90f * x;
        }
        float e = 0;
        while(e < 1)
        {
            float d = Time.deltaTime / duration;
            e += d;
            for (int i = 0; i < 8; i++)
                dialrots[i].localEulerAngles += new Vector3(0, rot[i % 4] * d, 0);
            yield return null;
        }
        for(int i = 0; i < 4; i++)
        {
            dialrots[i].localEulerAngles = new Vector3(0, 90 * (end[i] + 2), 0);
            dialrots[i + 4].localEulerAngles = new Vector3(0, 90 * end[i], 0);
        }
        pressable = !reset;
    }

    private IEnumerator Unlock()
    {
        moduleSolved = true;
        displays[0].text = "";
        yield return new WaitForSeconds(0.5f);
        Audio.PlaySoundAtTransform("Solve", transform);
        float e = 0;
        while(e < 1)
        {
            e += Time.deltaTime;
            door.localEulerAngles = new Vector3(0, -90, Mathf.Lerp(90, 240, e));
            yield return null;
        }
        module.HandlePass();
    }

    private IEnumerator Reset()
    {
        pressable = false;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, reset.transform);
        Audio.PlaySoundAtTransform("Reset", transform);
        while(pastpos.Count() > 0)
        {
            if(pastpos.Last() < 0)
            {
                Audio.PlaySoundAtTransform("Unlock", transform);
                unlock--;
                for (int i = 4 * unlock; i < 4 * (unlock + 1); i++)
                    pins[i].localPosition = new Vector3(0, 0, 0);
                pastpos.RemoveAt(pastpos.Count() - 1);
            }
            else
            {
                StartCoroutine(Rotate(tesseract[pos], tesseract[pastpos.Last()], 0.1f, true));
                yield return new WaitForSeconds(0.12f);
                pos = pastpos.Last();
                pastpos.RemoveAt(pastpos.Count() - 1);
                remaining++;
                displays[0].text = remaining.ToString();               
            }
        }
        reset.AddInteractionPunch(-1f);
        Audio.PlaySoundAtTransform("Lock", transform);
        pressable = true;
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <T/B><L/R> [Selects dial in the specified corner of the module. Chain with or without spaces.] | !{0} unlock | !{0} reset";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        if(command.ToLowerInvariant() == "unlock")
        {
            while (!pressable)
                yield return null;
            submit.OnInteract();
            yield break;
        }
        if (command.ToLowerInvariant() == "reset")
        {
            while (!pressable)
                yield return null;
            reset.OnInteract();
            yield break;
        }
        command = command.Replace(" ", "");
        if(command.Length % 2 > 0)
        {
            yield return "sendtochaterror!f Unpaired position found.";
            yield break;
        }
        command = command.ToUpperInvariant();
        List<int> d = new List<int> { };
        for(int i = 0; i < command.Length; i += 2)
        {
            int c = 0;
            string a = command[i].ToString();
            string b = command[i + 1].ToString();
            if ("TB".Contains(a))
                c = a == "T" ? 0 : 2;
            else
            {
                yield return "sendtochaterror!f \"" + a + "\" is not a valid position. Enter either T or B.";
                yield break;
            }
            if ("LR".Contains(b))
                c += b == "L" ? 0 : 1;
            else
            {
                yield return "sendtochaterror!f \"" + b + "\" is not a valid position. Enter either L or R.";
                yield break;
            }
            d.Add(c);
        }
        for(int i = 0; i < d.Count(); i++)
        {
            while (!pressable)
                yield return null;
            if (remaining < 1)
            {
                yield return "sendtochat!f Out of moves.";
                yield break;
            }
            dials[d[i]].OnInteract();
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (!pressable)
            yield return null;
        Audio.PlaySoundAtTransform("Unlock", transform);
        for (int i = 0; i < 12; i++)
        {
            float arg = Mathf.PI * int.Parse(pins[i].name) / 6f;
            pins[i].localPosition = new Vector3(Mathf.Sin(arg) * 0.09f, 0, Mathf.Cos(arg) * 0.09f);
        }
        StartCoroutine(Unlock());
    }
}