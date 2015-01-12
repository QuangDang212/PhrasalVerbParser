﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemmaSharp.Classes;
using OpenNLP.Tools.Parser;
using OpenNLP.Tools.Trees;

namespace PhrasalVerbParser.Src.Detectors
{
    class ParseBasedPhrasalVerbDetector
    {
        private readonly EnglishTreebankParser parser;
        private readonly Lemmatizer lemmatizer;

        public ParseBasedPhrasalVerbDetector(EnglishTreebankParser parser, Lemmatizer lemmatizer)
        {
            this.parser = parser;
            this.lemmatizer = lemmatizer;
        }

        public bool IsMatch(string sentence, PhrasalVerb phrasalVerb)
        {
            var dependencies = ComputeDependencies(sentence);
            
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
            return relevantRelationships.Any();
        }

        private IEnumerable<TypedDependency> ComputeDependencies(string sentence)
        {
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
    }
}
