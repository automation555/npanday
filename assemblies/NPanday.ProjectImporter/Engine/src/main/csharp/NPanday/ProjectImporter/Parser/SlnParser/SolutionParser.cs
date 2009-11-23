using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


/// Author: Leopoldo Lee Agdeppa III

namespace NPanday.ProjectImporter.Parser.SlnParser
{
    public sealed class SolutionParser
    {
        public delegate List<Dictionary<string, object>> ParserAlgoDelegate(System.IO.FileInfo solutionFile, ref string warningMsg);

        static ParserAlgoDelegate[] ALGORITHMS = 
        {
            new ProjectSolutionParser().Parse
        };


        public static List<Dictionary<string, object>> ParseSolution(FileInfo solutionFile, ref string warningMsg)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

            foreach (ParserAlgoDelegate algo in ALGORITHMS)
            {
               list.AddRange(algo(solutionFile, ref warningMsg));
            }

            return list;
        }

        
    }
}
