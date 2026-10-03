using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 「決まった構成の袋をシャッフルして、端から1個ずつ配る」方式のランダマイザー。
    /// 袋が空になったら、また同じ構成で作り直してシャッフルする。
    /// これにより「長い目で見ると必ず決まった比率で出る」かつ「短期的にはランダムに見える」を両立できる。
    /// </summary>
    public class ShuffleBag
    {
        readonly List<int> _template; // 1回分の袋の中身(例: 形なら7種、色なら4種×3個の12個)
        readonly List<int> _bag = new List<int>();

        public ShuffleBag(List<int> template)
        {
            _template = template;
        }

        public int Next()
        {
            if (_bag.Count == 0) Refill();
            int lastIndex = _bag.Count - 1;
            int value = _bag[lastIndex];
            _bag.RemoveAt(lastIndex);
            return value;
        }

        void Refill()
        {
            _bag.AddRange(_template);
            // Fisher-Yatesシャッフル
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
    }
}