
// 데이터 전송 객체 (Data Transfer Object) 모음
// ViewModel 과 View 간의 데이터 교환에만 사용
// 다른 계층에서는 사용하지 말 것
// public readonly struct 로 정의하여 불변(immutable) 객체로 사용하며,
// 멤버 변수는 get only property 로 정의

namespace IndoorMapTools.ViewModel
{
    public readonly struct FGAEdgeData
    {
        public FGAData From { get; }
        public FGAData To { get; }
        public bool IsDirected { get; }

        public FGAEdgeData(FGAData from, FGAData to) : this(from, to, true) {}
        public FGAEdgeData(FGAData from, FGAData to, bool isDirected)
        { From = from; To = to; IsDirected = isDirected; }
    }

    public readonly struct FGAData
    {
        public int F { get; }
        public int G { get; }
        public int A { get; }
        public FGAData(int f, int g, int a)
        { F = f; G = g; A = a; }
    }
}
