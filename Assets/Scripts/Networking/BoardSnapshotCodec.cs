namespace SandTetris
{
    /// <summary>
    /// SandGridの状態を、通信で送るための軽量なバイト配列に変換する。
    /// 1マス1バイトで、0=空、1〜4=色グループ(ColorIndex+1)を表す。
    /// 粒ごとの明暗ノイズ(見た目の質感)は送らない。相手の盤面は状況把握できれば十分、という判断。
    /// </summary>
    public static class BoardSnapshotCodec
    {
        /// <summary>
        /// 5状態(空+4色)しかないことを利用して、1マスを3ビットに詰め込んでエンコードする。
        /// 1バイトに詰め込むより約37.5%のサイズで済む。
        /// Fusionの Networked配列(位置ベースで差分を検出する仕組み)と組み合わせて使う前提。
        /// 操作中のミノは含めない(位置がなめらかに動くため、別の軽量なチャンネルで高頻度に送る)。
        /// </summary>
        public static byte[] EncodePacked(SandGrid grid)
        {
            var raw = new byte[grid.Cells.Length];
            for (int i = 0; i < grid.Cells.Length; i++)
            {
                var cell = grid.Cells[i];
                // 0=空、1〜4=色グループ、5=コンクリート、6=灰(どれも3ビットで表せる範囲なので、通信量は増えない)
                if (cell.IsConcrete) raw[i] = 5;
                else if (cell.IsAsh) raw[i] = 6;
                else raw[i] = cell.Occupied ? (byte)(cell.ColorIndex + 1) : (byte)0;
            }
            return PackBits(raw);
        }

        static byte[] PackBits(byte[] raw)
        {
            int totalBytes = (raw.Length * 3 + 7) / 8;
            var data = new byte[totalBytes];

            int bitPos = 0;
            foreach (var value in raw)
            {
                WriteBits(data, bitPos, value);
                bitPos += 3;
            }
            return data;
        }

        /// <summary>EncodePacked の逆変換。cellCount 個の 0〜4 の値の配列に戻す。</summary>
        public static byte[] DecodePacked(byte[] packed, int cellCount)
        {
            var result = new byte[cellCount];
            int bitPos = 0;
            for (int i = 0; i < cellCount; i++)
            {
                result[i] = ReadBits(packed, bitPos);
                bitPos += 3;
            }
            return result;
        }

        static void WriteBits(byte[] data, int bitPos, byte value)
        {
            for (int i = 0; i < 3; i++)
            {
                int bit = (value >> (2 - i)) & 1;
                int bytePos = (bitPos + i) / 8;
                int bitOffset = 7 - ((bitPos + i) % 8);
                if (bit == 1) data[bytePos] |= (byte)(1 << bitOffset);
            }
        }

        static byte ReadBits(byte[] data, int bitPos)
        {
            byte value = 0;
            for (int i = 0; i < 3; i++)
            {
                int bytePos = (bitPos + i) / 8;
                int bitOffset = 7 - ((bitPos + i) % 8);
                int bit = (data[bytePos] >> bitOffset) & 1;
                value = (byte)((value << 1) | bit);
            }
            return value;
        }

        /// <summary>
        /// ピクセル単位ではなく、blockSize×blockSizeの「テトリスのマス」単位に間引いてエンコードする。
        /// 各マスの中で一番多い状態(空 or どの色か)を代表値として採用する。
        /// Fusionの RPC は1回あたり512バイトまでという制限があり、ピクセル単位(数千〜数万バイト)の
        /// 生データはそのままでは送れないため、この間引き版を通信用に使う。
        /// </summary>
        public static byte[] EncodeCoarse(SandGrid grid, int blockSize, int widthInBlocks, int heightInBlocks)
        {
            var data = new byte[widthInBlocks * heightInBlocks];
            var counts = new int[5]; // [0]=空、[1..4]=ColorIndex+1

            for (int by = 0; by < heightInBlocks; by++)
            {
                for (int bx = 0; bx < widthInBlocks; bx++)
                {
                    System.Array.Clear(counts, 0, counts.Length);
                    int startX = bx * blockSize;
                    int startY = by * blockSize;

                    for (int dx = 0; dx < blockSize; dx++)
                    {
                        for (int dy = 0; dy < blockSize; dy++)
                        {
                            int x = startX + dx;
                            int y = startY + dy;
                            if (!grid.InBounds(x, y)) continue;

                            var cell = grid.Cells[grid.Index(x, y)];
                            counts[cell.Occupied ? cell.ColorIndex + 1 : 0]++;
                        }
                    }

                    int best = 0;
                    for (int i = 1; i < counts.Length; i++)
                    {
                        if (counts[i] > counts[best]) best = i;
                    }
                    data[by * widthInBlocks + bx] = (byte)best;
                }
            }
            return data;
        }
    }
}