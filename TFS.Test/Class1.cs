using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TFS.Test
{
    [TestFixture]
    public class Class1
    {

        [Test]
        public void Foo()
        {
            var test = new Export.Branches.TFS();
            test.Go();
        }
    }
}
