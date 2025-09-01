// ===============================================================
// SEMANA 11 - Traductor básico ES ↔ EN
// Autor: (Tu nombre) - 3er semestre TI
// Modo clásico con Main() para evitar CS8803 (sin top-level).
// ===============================================================

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

namespace Semana11
{
    // --------- Estructura de exportación a JSON ----------
    class Entry
    {
        public List<string> es { get; set; } = new();
        public List<string> en { get; set; } = new();
    }

    // --------- Diccionario + persistencia ----------
    class Lexicon
    {
        private readonly Dictionary<string, List<string>> esToEn = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> enToEs = new(StringComparer.OrdinalIgnoreCase);
        private readonly string storePath;

        public Lexicon(string filePath)
        {
            storePath = filePath;
            if (System.IO.File.Exists(storePath)) Load();
            else Seed(); // cargo palabras base (≥10)
        }

        // Normalizo (minúsculas + quito tildes) para búsquedas robustas
        private static string Norm(string s)
        {
            var nf = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in nf)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static void AddMap(Dictionary<string, List<string>> map, string key, IEnumerable<string> values)
        {
            key = Norm(key);
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<string>();
                map[key] = list;
            }
            foreach (var v in values)
                if (!list.Contains(v, StringComparer.OrdinalIgnoreCase))
                    list.Add(v);
        }

        public void AddEsToEn(string es, IEnumerable<string> enList)
        {
            AddMap(esToEn, es, enList);
            foreach (var en in enList) AddMap(enToEs, en, new[] { es });
        }
        public void AddEnToEs(string en, IEnumerable<string> esList)
        {
            AddMap(enToEs, en, esList);
            foreach (var es in esList) AddMap(esToEn, es, new[] { en });
        }

        public string? LookupEsToEn(string word)
        {
            var key = Norm(word);
            return esToEn.TryGetValue(key, out var list) && list.Count > 0 ? list[0] : null;
        }
        public string? LookupEnToEs(string word)
        {
            var key = Norm(word);
            return enToEs.TryGetValue(key, out var list) && list.Count > 0 ? list[0] : null;
        }

        public void Save()
        {
            var entries = new List<Entry>();
            foreach (var kv in esToEn)
                entries.Add(new Entry { es = new() { kv.Key }, en = kv.Value.ToList() });
            foreach (var kv in enToEs)
                entries.Add(new Entry { en = new() { kv.Key }, es = kv.Value.ToList() });

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(storePath, json, Encoding.UTF8);
        }

        private void Load()
        {
            var json = System.IO.File.ReadAllText(storePath, Encoding.UTF8);
            var entries = JsonSerializer.Deserialize<List<Entry>>(json) ?? new();
            foreach (var e in entries)
            {
                foreach (var s in e.es) AddMap(esToEn, s, e.en);
                foreach (var s in e.en) AddMap(enToEs, s, e.es);
            }
        }

        private void Seed()
        {
            AddEsToEn("tiempo", new[] { "time" });
            AddEsToEn("persona", new[] { "person" });
            AddEsToEn("año", new[] { "year" });
            AddEsToEn("camino", new[] { "way" });
            AddEsToEn("forma", new[] { "way" });
            AddEsToEn("día", new[] { "day" });
            AddEsToEn("cosa", new[] { "thing" });
            AddEsToEn("hombre", new[] { "man" });
            AddEsToEn("mundo", new[] { "world" });
            AddEsToEn("vida", new[] { "life" });
            AddEsToEn("mano", new[] { "hand" });
            AddEsToEn("parte", new[] { "part" });
            AddEsToEn("niño", new[] { "child" });
            AddEsToEn("niña", new[] { "child" });
            AddEsToEn("ojo", new[] { "eye" });
            AddEsToEn("mujer", new[] { "woman" });
            AddEsToEn("lugar", new[] { "place" });
            AddEsToEn("trabajo", new[] { "work" });
            AddEsToEn("semana", new[] { "week" });
            AddEsToEn("caso", new[] { "case" });
            AddEsToEn("punto", new[] { "point" });
            AddEsToEn("tema", new[] { "point" });
            AddEsToEn("gobierno", new[] { "government" });
            AddEsToEn("empresa", new[] { "company" });
            AddEsToEn("compañía", new[] { "company" });
            Save();
        }
    }

    class Program
    {
        // Mantener estilo de mayúsculas del original
        static string ApplyCasing(string source, string target)
        {
            if (source.All(char.IsUpper)) return target.ToUpperInvariant();
            if (source.Length > 0 && char.IsUpper(source[0]))
                return char.ToUpperInvariant(target[0]) + (target.Length > 1 ? target[1..] : "");
            return target.ToLowerInvariant();
        }

        // Tokenizo: palabras vs no-palabras (puntuación/espacios)
        static IEnumerable<(string token, bool isWord)> Tokenize(string text)
        {
            var parts = Regex.Split(text, @"(\p{L}+|\p{Nd}+)");
            foreach (var p in parts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                bool isWord = Regex.IsMatch(p, @"^\p{L}+|\p{Nd}+$");
                yield return (p, isWord);
            }
        }

        static string TranslateSentence(string sentence, bool esToEn, Lexicon lex)
        {
            var sb = new StringBuilder();
            foreach (var (tok, isWord) in Tokenize(sentence))
            {
                if (!isWord) { sb.Append(tok); continue; }
                string? tr = esToEn ? lex.LookupEsToEn(tok) : lex.LookupEnToEs(tok);
                sb.Append(tr is null ? tok : ApplyCasing(tok, tr));
            }
            return sb.ToString();
        }

        // --------- Punto de entrada (evita CS8803) ----------
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var dic = new Lexicon("diccionario.json");

            while (true)
            {
                Console.WriteLine("\n================= MENÚ =================");
                Console.WriteLine("1. Traducir una frase");
                Console.WriteLine("2. Agregar palabras al diccionario");
                Console.WriteLine("0. Salir");
                Console.Write("Seleccione una opción: ");

                var opt = Console.ReadLine()?.Trim();
                if (opt == "0") { Console.WriteLine("¡Hasta luego!"); break; }

                switch (opt)
                {
                    case "1":
                        Console.Write("Dirección (1=Español→Inglés, 2=Inglés→Español): ");
                        bool esToEn = (Console.ReadLine()?.Trim() == "1");
                        Console.Write("Ingrese la frase: ");
                        var frase = Console.ReadLine() ?? "";
                        var traducida = TranslateSentence(frase, esToEn, dic);
                        Console.WriteLine($"\nTraducción (parcial si faltan palabras):\n{traducida}\n");
                        break;

                    case "2":
                        Console.Write("Agregar (1=Español→Inglés, 2=Inglés→Español): ");
                        var dir = Console.ReadLine()?.Trim();

                        if (dir == "1")
                        {
                            Console.Write("Palabra en español: ");
                            var es = (Console.ReadLine() ?? "").Trim();
                            Console.Write("Traducciones en inglés (separadas por coma): ");
                            var en = (Console.ReadLine() ?? "")
                                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (es.Length > 0 && en.Length > 0)
                            {
                                dic.AddEsToEn(es, en);
                                dic.Save();
                                Console.WriteLine("✅ Añadido.");
                            }
                            else Console.WriteLine("⚠️ Datos incompletos.");
                        }
                        else if (dir == "2")
                        {
                            Console.Write("Word in English: ");
                            var en = (Console.ReadLine() ?? "").Trim();
                            Console.Write("Traducciones al español (separadas por coma): ");
                            var es = (Console.ReadLine() ?? "")
                                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (en.Length > 0 && es.Length > 0)
                            {
                                dic.AddEnToEs(en, es);
                                dic.Save();
                                Console.WriteLine("✅ Añadido.");
                            }
                            else Console.WriteLine("⚠️ Datos incompletos.");
                        }
                        else Console.WriteLine("Opción inválida.");
                        break;

                    default:
                        Console.WriteLine("Opción inválida.");
                        break;
                }
            }
        }
    }
}


