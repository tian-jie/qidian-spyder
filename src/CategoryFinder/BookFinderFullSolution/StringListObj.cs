using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class StringListObj : ListObject<string>
    {
        public StringListObj(string name)
            : base(name)
        {
        }
        public override void Deserialize()
        {
            ConsoleLogger.Debug("{0} want to lock in Deserialize", _name);
            lock (_locker)
            {
                ConsoleLogger.Debug("{0} locked in Deserialize", _name);
                if (File.Exists($"data/{_name}.dat"))
                {
                    using (var sr = new StreamReader(path: $"data/{_name}.dat", encoding: Encoding.UTF8))
                    {
                        var s = sr.ReadToEnd();
                        var l = s.Split('\n');
                        _list = l.ToHashSet();
                    }
                }
                ConsoleLogger.Debug("{0} released in Deserialize", _name);
            }
        }

        override public void Serialize()
        {
            StringBuilder sb = new StringBuilder();
            ConsoleLogger.Debug("{0} want to lock in Serialize", _name);
            lock (_locker)
            {
                ConsoleLogger.Debug("{0} locked in Serialize", _name);
                foreach (var item in _list)
                {
                    sb.Append(item.ToString() + '\n');
                }
                ConsoleLogger.Debug("{0} released in Serialize", _name);
            }

            //Task.Run(() =>
            //{
                using (var sw = new StreamWriter(path: $"data/{_name}.dat", append: false, encoding: Encoding.UTF8))
                {
                    sw.Write(sb.ToString());
                }
            //});
        }


    }

}
