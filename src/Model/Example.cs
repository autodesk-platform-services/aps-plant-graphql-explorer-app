using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQLClient.Data
{
    internal class ExamplesObject
    {
        public List<Example> Examples { get; set; }
    }

    internal class Example
    {
        public string Title { get; set; }
        public string Scope { get; set; }
        public string Script { get; set; }
        public string Project { get; set; }
    }
}
