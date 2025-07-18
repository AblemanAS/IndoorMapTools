using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Helper
{
    /// <summary>
    /// Win32 Native Library 를 통해 URI 이미지를 커서 객체로 로드하기 위한 클래스입니다.
    /// </summary>
    public class Win32CursorLoader
    {
        /// <summary> Win32 Native Library 를 통해 URI 이미지를 커서 객체로 로드합니다. </summary>
        /// <param name="uri"> 원본 png URI 문자열 </param>
        /// <param name="xHotspot"> 0.0 ~ 1.0 상대좌표 (래스터 좌표계) </param>
        /// <param name="yHotspot"> 0.0 ~ 1.0 상대좌표 (래스터 좌표계) </param>
        /// <param name="scaleFactor"> 시스템 기본 커서 대비 크기 비율 </param>
        /// <returns> WPF Cursor 객체 </returns>
        public static Cursor Load(string uri, double xHotspot = 0.0, double yHotspot = 0.0, double scaleFactor = 1.0)
        {
            // 커서 이미지 로드
            var cursorUri = new Uri(uri);
            var cursorStream = Application.GetResourceStream(cursorUri).Stream;
            var originalBitmap = new BitmapImage();
            originalBitmap.BeginInit();
            originalBitmap.StreamSource = cursorStream;
            originalBitmap.EndInit();

            // 이미지 스케일
            var imgTransform = new ScaleTransform();
            imgTransform.ScaleX = SystemParameters.CursorWidth * scaleFactor / originalBitmap.Width;
            imgTransform.ScaleY = SystemParameters.CursorHeight * scaleFactor / originalBitmap.Height;
            var bitmap = new TransformedBitmap(originalBitmap, imgTransform);

            // 커서 핸들을 생성하고 커서로 설정
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);

            // Win32 커서 객체 변수 초기화
            SafeIconHandle cursorHandle = null;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hBitmapMask = IntPtr.Zero;

            try
            {
                // 컬러 비트맵
                byte[] pixels = new byte[height * stride];
                bitmap.CopyPixels(pixels, stride, 0);
                hBitmap = CreateBitmapFromPixels(pixels, width, height, stride, bitmap.Format);

                // 마스크 비트맵
                byte[] maskPixels = new byte[height * stride];
                hBitmapMask = CreateBitmapFromPixels(maskPixels, width, height, stride, bitmap.Format);

                // Hotspot 설정
                int xHotspotInt = (int)(width * xHotspot);
                int yHotspotInt = (int)(height * yHotspot);
                if(xHotspotInt > width - 1) xHotspotInt = width - 1;
                if(yHotspotInt > height - 1) yHotspotInt = height - 1;

                // Win32 커서 객체 생성
                IconInfo iconInfo = new IconInfo
                {
                    fIcon = false,
                    xHotspot = xHotspotInt,
                    yHotspot = yHotspotInt,
                    hbmMask = hBitmapMask,
                    hbmColor = hBitmap
                };
                IntPtr hIcon = CreateIconIndirect(ref iconInfo);

                // 핸들 생성
                if(hIcon != IntPtr.Zero)
                    cursorHandle = new SafeIconHandle(hIcon, true);
            }
            finally
            {
                if(hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if(hBitmapMask != IntPtr.Zero) DeleteObject(hBitmapMask);
            }

            return CursorInteropHelper.Create(cursorHandle);
        }

        // Win32 커서 객체 정보 구조체
        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            public bool fIcon;      // true: Icon / false: Cursor
            public int xHotspot;    // 기준점 X
            public int yHotspot;    // 기준점 Y
            public IntPtr hbmMask;  // 마스크 비트맵
            public IntPtr hbmColor; // 아이콘 컬러 비트맵
        }

        private static IntPtr CreateBitmapFromPixels(byte[] pixels, int width, int height, int stride, PixelFormat bitmapFormat)
        {
            IntPtr hBitmap;
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

            try { hBitmap = CreateBitmap(width, height, 1, (uint)bitmapFormat.BitsPerPixel, handle.AddrOfPinnedObject()); }
            finally { handle.Free(); }

            return hBitmap;
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        private class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeIconHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
                => SetHandle(preexistingHandle);
            protected override bool ReleaseHandle() => DestroyIcon(handle);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DestroyIcon(IntPtr hIcon);
        }
    }
}
