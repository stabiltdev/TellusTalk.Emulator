using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Stabilt.TellusTalk.Emulator.Ids;

internal static class GuidExtensions
{
    private static readonly DateTimeOffset GregorianReform = new(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

    extension(Guid)
    {
        /// <summary>A version 1 UUID, the form TellusTalk's ids take, timed by the system clock as <see cref="Guid.CreateVersion7()"/> is.</summary>
        [SuppressMessage(
            "ApiDesign",
            "RS0030:Do not use banned APIs",
            Justification = "Mirrors Guid.CreateVersion7(), which takes the system clock when given no time.")]
        public static Guid CreateVersion1() => Guid.CreateVersion1(DateTimeOffset.UtcNow);

        /// <summary>
        /// A version 1 UUID for <paramref name="timestamp"/>. .NET reads and writes version 1 but mints none. The clock
        /// sequence and the node are drawn at random for each id, which keeps ids of the same tick apart without the
        /// state a counter would need.
        /// </summary>
        public static Guid CreateVersion1(DateTimeOffset timestamp)
        {
            var ticks = (timestamp - GregorianReform).Ticks;
            var timeLow = (uint)ticks;
            var timeMid = (ushort)(ticks >> 32);
            var timeHighAndVersion = (ushort)(((ticks >> 48) & 0x0FFF) | 0x1000);

            Span<byte> random = stackalloc byte[8];
            RandomNumberGenerator.Fill(random);

            // The two top bits of the clock sequence mark RFC 4122's variant; the node's multicast bit marks it as no
            // network address, as RFC 4122 asks of a random node.
            var clockSequenceHighAndVariant = (byte)((random[0] & 0x3F) | 0x80);
            var firstNodeOctet = (byte)(random[2] | 0x01);

            return new Guid(
                timeLow,
                timeMid,
                timeHighAndVersion,
                clockSequenceHighAndVariant,
                random[1],
                firstNodeOctet,
                random[3],
                random[4],
                random[5],
                random[6],
                random[7]);
        }
    }
}
