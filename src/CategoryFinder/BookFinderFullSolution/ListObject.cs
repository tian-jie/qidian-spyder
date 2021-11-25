using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    abstract public class ListObject<T>
    {
        internal object _locker = new object();
        internal HashSet<T> _list = new HashSet<T>();
        internal string _name = "";

        public ListObject(string name)
        {
            _name = name;
        }

        virtual public void EnsureCapacity(int capacity)
        {
            _list.EnsureCapacity(capacity);
        }

        virtual public T GetOne()
        {
            while (true)
            {
                //ConsoleLogger.Debug("{0} want to lock in GetOne", _name);
                lock (_locker)
                {
                    //ConsoleLogger.Debug("{0} locked in GetOne", _name);
                    if (_list.Count > 0)
                    {
                        var item = _list.Take(1);
                        var s = item.First();
                        _list.Remove(s);
                        //ConsoleLogger.Debug("{0} released in GetOne", _name);
                        return s;
                    }

                    //ConsoleLogger.Debug("{0} not get in GetOne", _name);
                }

                Thread.Sleep(1000);
            }
        }


        virtual public void AddOne(T t)
        {
            //Console.WriteLine("{0} want to lock in AddOne", _name);
            lock (_locker)
            {
                //Console.WriteLine("{0} locked in AddOne", _name);
                _list.Add(t);
                //Console.WriteLine("{0} released in AddOne", _name);
            }
        }
        virtual public void AddMany(List<T> items)
        {
            //Console.WriteLine("{0} want to lock in AddOne", _name);
            lock (_locker)
            {
               foreach(var item in items)
                {
                    _list.Add(item);
                }
            }
        }



        abstract public void Deserialize();

        abstract public void Serialize();

        virtual public void Archive(int cnt)
        {
            throw new NotImplementedException();
        }
        virtual public void Dearchive(int cnt)
        {
            throw new NotImplementedException();
        }

        virtual public int Count()
        {
            return _list.Count;
        }

        virtual public List<T> GetList(int count = -1)
        {
            ConsoleLogger.Debug("{0} want to locked in GetList", _name);
            lock (_locker)
            {
                ConsoleLogger.Debug("{0} locked in GetList", _name);
                List<T> list = null;
                var cnt = _list.Count;
                if (count == -1)
                {
                    count = cnt;
                }

                if (cnt == 0)
                {
                    list = new List<T>();
                }
                else if (cnt < count)
                {
                    list = new List<T>(_list);
                    _list.Clear();
                    return list;
                }
                else
                {
                    var ls = _list.Take(count);
                    foreach (var l in ls)
                    {
                        _list.Remove(l);
                    }
                    list = ls.ToList();

                }
                ConsoleLogger.Debug("{0} released in GetList", _name);
                return list;
            }
        }


        virtual public bool Contains(T item)
        {
            return _list.Contains(item);
        }

    }


}
