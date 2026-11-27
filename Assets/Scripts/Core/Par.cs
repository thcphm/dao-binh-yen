// Chay song song khi co luong phu (Windows, Editor); WebGL khong co luong phu -> chay tuan tu tren main thread.
// Moi vong lap dung Par.For chi ghi vao o rieng cua chi so i, nen ket qua giong het nhau o ca hai che do.
using System;
using System.Threading.Tasks;

namespace SereneIsle
{
    public static class Par
    {
        /// <summary>Co dung luong phu khong. Test co the tat de thu duong chay cua WebGL ngay trong Editor.</summary>
#if UNITY_WEBGL && !UNITY_EDITOR
        public static bool Threads = false;
#else
        public static bool Threads = true;
#endif

        public static void For(int from, int to, Action<int> body)
        {
            if (Threads) Parallel.For(from, to, body);
            else for (int i = from; i < to; i++) body(i);
        }
    }
}
