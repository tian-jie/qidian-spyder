using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class NonStringListObj<T> : ListObject<T>
    {
        public NonStringListObj(string name)
            : base(name)
        {
        }
        // 重写写文件的方式

        public override void Deserialize()
        {
            lock (_locker)
            {
                if (File.Exists($"data/{_name}.dat"))
                {
                    using (var sr = new StreamReader(path: $"data/{_name}.dat", encoding: Encoding.UTF8))
                    {
                        var s = sr.ReadToEnd();
                        _list = Newtonsoft.Json.JsonConvert.DeserializeObject<HashSet<T>>(s);
                    }
                }
            }
        }

        public override void Serialize()
        {
            var s = "";
            lock (_locker)
            {
                s = Newtonsoft.Json.JsonConvert.SerializeObject(_list);
            }

            Task.Run(() =>
            {
                using (var sw = new StreamWriter(path: $"data/{_name}.dat", append: false, encoding: Encoding.UTF8))
                {
                    sw.Write(s);
                }
            });
        }

    }


}
