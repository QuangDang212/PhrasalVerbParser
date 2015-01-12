﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Serialization;
using LemmaSharp.Classes;
using OpenNLP.Tools.Parser;
using OpenNLP.Tools.Tokenize;
using OpenNLP.Tools.Trees;
using PhrasalVerbParser.Src;
using PhrasalVerbParser.Src.Detectors;

namespace PhrasalVerbParser
{
    class Program
    {
        private static readonly string PathToApplication = Directory.GetCurrentDirectory() + "/../../";

        
        static void Main(string[] args)
        {
            var usingEnglishParser = new UsingEnglishParser();
            var allPhrasalVerbs = usingEnglishParser.ParseAllPhrasalVerbs();
            Console.Write("Parsed {0} phrasal verbs on using english");

            // Persist phrasal verbs
            var phrasalVerbFilePath = PathToApplication + "Resources/phrasalVerbs";
            PersistPhrasalVerbs(allPhrasalVerbs, phrasalVerbFilePath);
            Console.WriteLine("Phrasal verbs persisted");

            // Lemmatizer
            var fullPathToSrcData = PathToApplication + "Resources/lemmatizer/en_lemmatizer_data.lem";
            var stream = File.OpenRead(fullPathToSrcData);
            var lemmatizer = new Lemmatizer(stream);
            
            // load phrasal verbs & examples
            var phrasalVerbs = ReadPhrasalVerbs(phrasalVerbFilePath);

            //
            var tokenizerModelPaths = PathToApplication + "Resources/OpenNlp/Models/EnglishTok.nbin";
            var tokenizer = new EnglishMaximumEntropyTokenizer(tokenizerModelPaths);
            var basicDetector = new BasicPhrasalVerbDetector(tokenizer, lemmatizer);
            var parseBasedDetector = new ParseBasedPhrasalVerbDetector(GetParser(), lemmatizer);

            // detect the phrasal verbs in the examples
            var results = new List<Tuple<string, string, bool, bool>>();
            foreach (var phrasalVerb in phrasalVerbs)
            {
                foreach (var usage in phrasalVerb.Usages)
                {
                    // compute dependencies
                    var example = LowerCaseAllUpperCasedWords(usage.Example);

                    var isDetectedByBasic = basicDetector.IsMatch(example, phrasalVerb);
                    var isDetectedByParseBased = parseBasedDetector.IsMatch(example, phrasalVerb);
                    results.Add(new Tuple<string, string, bool, bool>(phrasalVerb.Name, example, isDetectedByBasic, isDetectedByParseBased));
                    /*var dependencies = ComputeDependencies(example);

                    Console.WriteLine("{0} -> {1}", phrasalVerb.Name, example);
                    // get relevant dependencies found
                    var parts = phrasalVerb.Name.Split(' ');
                    var root = parts.First();
                    // We take only the 2nd part
                    // For phrasal verbs with several particles, that's a good approximation for now
                    // (we could check that all the particles are also linked)
                    var last = parts[1];
                    var relevantRelationships = dependencies
                        .Where(d => (root == lemmatizer.Lemmatize(d.Dep().GetWord()) && last == d.Gov().GetWord())
                                    || (root == lemmatizer.Lemmatize(d.Gov().GetWord()) && last == d.Dep().GetWord()))
                                    .ToList();
                    results.Add(new Tuple<string, string, List<TypedDependency>>(phrasalVerb.Name, example, relevantRelationships));*/
                }
            }
            Console.WriteLine("===========================");

            // Print results
            Console.WriteLine("{0} ({1}%) detected by basic detector", results.Count(tup => tup.Item3), (float)(results.Count(tup => tup.Item3) * 100) / results.Count);
            Console.WriteLine("{0} ({1}) detected by parse based detector", results.Count(tup => tup.Item4), (float)(results.Count(tup => tup.Item4) * 100) / results.Count);
            Console.WriteLine("----------");
            Console.WriteLine("Missed basic detection:");
            foreach (var result in results.Where(tup => !tup.Item3))
            {
                Console.WriteLine("{0} - {1}", result.Item1, result.Item2);
            }
            Console.WriteLine("----------");
            Console.WriteLine("Missed parse based detection:");
            foreach (var result in results.Where(tup => !tup.Item4))
            {
                Console.WriteLine("{0} - {1}", result.Item1, result.Item2);
            }
            

            /*// persisting list of phrasal verbs
            Console.WriteLine("============");
            Console.WriteLine("Persisting phrasal verbs to {0}", phrasalVerbFilePath);
            PersistPhrasalVerbs(phrasalVerbs, phrasalVerbFilePath);
            Console.WriteLine("Persisted phrasal verbs");*/

            // persisting examples
            /*var pvExamplesFilePath = PathToApplication + "Resources/phrasalVerbsExamples.txt";
            PersistPhrasalVerbsAndExamples(phrasalVerbs, pvExamplesFilePath);
            Console.WriteLine("Persisted examples");
            Console.WriteLine("-------------------------");*/
            
            // stats on usingenglish phrasal verbs
            /*var verbs = ReadPhrasalVerbs(phrasalVerbFilePath);
            var nbOfSeparableVerbs = verbs.Count(v => v.Usages.All(u => u.SeparableMandatory));
            var nbOfInseparableVerbs = verbs.Count(v => v.Usages.All(u => u.Inseparable));
            Console.WriteLine("{0} separable verbs", nbOfSeparableVerbs);
            Console.WriteLine("{0} inseparable verbs", nbOfInseparableVerbs);
            Console.WriteLine("{0} verbs", verbs.Count);*/

            // write a file with examples of phrasal verbs
            /*var pathToOutputFile = PathToApplication + "Resources/phrasalVerbExamples.txt";
            var verbs = ReadPhrasalVerbs(phrasalVerbFilePath);
            var lines =
                verbs.SelectMany(v => v.Usages.Select(u => new {Usage = u, v.Name}))
                    .Select(a => string.Format("{0}|{1}", a.Name, a.Usage.Example));
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            File.WriteAllLines(pathToOutputFile, lines);*/

            Console.WriteLine("=== END ===");
            Console.ReadKey();
        }

        private static EnglishTreebankParser _parser;
        private static EnglishTreebankParser GetParser()
        {
            if (_parser == null)
            {
                var modelPath = PathToApplication + "Resources/OpenNlp/Models/";
                _parser = new EnglishTreebankParser(modelPath, true, false);
            }
            return _parser;
        }

        private static IEnumerable<TypedDependency> ComputeDependencies(string sentence)
        {
            var parser = GetParser();
            var parse = parser.DoParse(sentence);
            // Extract dependencies from lexical tree
            var tlp = new PennTreebankLanguagePack();
            var gsf = tlp.GrammaticalStructureFactory();
            var tree = new ParseTree(parse);
            //Console.WriteLine(tree);
            try
            {
                var gs = gsf.NewGrammaticalStructure(tree);
                return gs.TypedDependencies();
            }
            catch (Exception)
            {
                Console.WriteLine("Exception when computing deps for {0}", sentence);
                return new List<TypedDependency>();
            }
        }

        private static string LowerCaseAllUpperCasedWords(string sentence)
        {
            var replacedSentence = Regex.Replace(sentence, "[A-Z]{2,}", s => s.Value.ToLower());
            return replacedSentence;
        }


        // Reading / Writing phrasal verbs ----------------------------

        private static List<string> ReadFleexPhrasalVerbs()
        {
            var pathToFile = PathToApplication + "Resources/fleexPhrasalVerbs.txt";
            var allLines = File.ReadAllLines(pathToFile).ToList();
            return allLines;
        }

        
        private static void PersistPhrasalVerbs(List<PhrasalVerb> verbs, string filePath)
        {
            using (var fs = File.OpenWrite(filePath))
            {
                var serializer = new BinaryFormatter();
                serializer.Serialize(fs, verbs);
            }
        }

        private static List<PhrasalVerb> ReadPhrasalVerbs(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                var serializer = new BinaryFormatter();
                var phrasalVerbs = (List<PhrasalVerb>) serializer.Deserialize(fs);
                return phrasalVerbs;
            }
        }
    }
}
