using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class ComplexStringListObj : StringListObj
    {
        private StringListObj _mainStringList;
        private StringListObj _tempStringList;

        public ComplexStringListObj(string name)
            : base(name)
        {
            _mainStringList = new StringListObj(name);
            _tempStringList = new StringListObj(name + "-tmp");
        }
        public override void EnsureCapacity(int capacity)
        {
            _mainStringList.EnsureCapacity(capacity);
        }


        // 重写写文件的方式
        public override void Deserialize()
        {
            _mainStringList.Deserialize();
            _tempStringList.Deserialize();
        }

        public void Serialize(bool isFull = false)
        {
            if (isFull)
            {
                _mainStringList.Serialize();
            }
            _tempStringList.Serialize();
        }

        public override void Archive(int cnt = -1)
        {
            var count = _tempStringList.Count();

            if (count < cnt)
            {
                return;
            }
            if (cnt == -1)
            {
                cnt = count;
            }

            StringBuilder sb = new StringBuilder();
            var items = _tempStringList.GetList(cnt);

            _mainStringList.AddMany(items);

            foreach (var item in items)
            {
                sb.Append(item.ToString() + '\n');
            }


            //Task.Factory.StartNew(() =>
            //{
                using (var sw = new StreamWriter(path: $"data/{_mainStringList._name}.dat", append: true, encoding: Encoding.UTF8))
                {
                    sw.Write(sb.ToString());
                }
            //});
        }

        public override void Dearchive(int cnt)
        {
            var count = _mainStringList.Count();
            if (count < cnt)
            {
                cnt = count;
            }
            // 这个比较麻烦，从main里读取一部分，然后文件里给读取出来的这部分删掉，再重新保存
            for (var i = 0; i < cnt; i++)
            {
                var item = _mainStringList.GetOne();
                _tempStringList.AddOne(item);
            }

            _mainStringList.Serialize();
            _tempStringList.Serialize();
        }


        public int Count(bool isMain)
        {
            if (isMain)
            {
                return _mainStringList.Count();
            }
            else
            {
                return _tempStringList.Count();
            }
        }

        public override void AddOne(string s)
        {
            _tempStringList.AddOne(s);
        }
        public override string GetOne()
        {
            return _tempStringList.GetOne();
        }

        public List<string> GetList(int cnt = -1, bool isMain = false)
        {
            if (isMain)
            {
                return _mainStringList.GetList();
            }
            else
            {
                return _tempStringList.GetList();
            }
        }

        public override bool Contains(string item)
        {
            return _mainStringList.Contains(item);
        }

    }


}
