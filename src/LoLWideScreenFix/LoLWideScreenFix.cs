using System;
using System.Numerics;
using System.IO;
using LoLWideScreenFix.Extensions;
using LeagueToolkit.IO.PropertyBin;
using LeagueToolkit.IO.PropertyBin.Properties;
using LeagueToolkit.Helpers.Hashing;

namespace LoLWideScreenFix
{
    /// <summary>
    /// Provides static methods for adapting UI elements for (Ultra)WideScreen resolutions.
    /// </summary>
    public static class LoLWideScreenFix
    {
        #region Hashes
        /// <summary>
        /// Hash for "mAnchors" for later comparisons
        /// </summary>
        private static readonly uint mAnchorsNameHash = Fnv1a.HashLower("mAnchors");

        /// <summary>
        /// Hash for "Anchor" for later comparisons
        /// </summary>
        private static readonly uint AnchorNameHash = Fnv1a.HashLower("Anchor");

        /// <summary>
        /// Hash for "mRect" for later comparisons
        /// </summary>
        private static readonly uint mRectNameHash = Fnv1a.HashLower("mRect");

        /// <summary>
        /// Hash for "mRectSourceResolutionWidth" for later comparisons
        /// </summary>
        private static readonly uint mRectSourceResolutionWidthNameHash = Fnv1a.HashLower("mRectSourceResolutionWidth");
        #endregion

        /// <summary>
        /// Max value LOL will allow vertically from the anchor point
        /// e.g. if anchor point is 1,1 and Rect start is 0,0 then
        /// rect will be placed MAGIC_VALUE pixels from the right-most
        /// screen edge
        /// </summary>
        /// <remarks>https://github.com/tnajdek/lol-eyefinity-surround-fixhud/blob/master/fixhud.py#L20</remarks>
        private const double MAGIC_VALUE = 1440.0;

        /// <summary>
        /// Creates a modified <see cref="BinTree"/> where the UI elements are center-aligned.
        /// </summary>
        /// <param name="fileLocation">Path to the WAD file.</param>
        /// <param name="targetResolutionWidth">Width of the resolution to be achieved.</param>
        /// <param name="changes">Number of changes made.</param>
        /// <returns>A <see cref="BinTree"/> adjusted to the line width of the resolution.</returns>
        public static BinTree GetModdedBinTree(string fileLocation, uint targetResolutionWidth, out int changes)
            => GetModdedBinTree(File.OpenRead(fileLocation), targetResolutionWidth, out changes);

        /// <summary>
        /// Creates a modified <see cref="BinTree"/> where the UI elements are center-aligned.
        /// </summary>
        /// <param name="entryStream">Wad entry stream</param>
        /// <param name="targetResolutionWidth">Width of the resolution to be achieved.</param>
        /// <param name="changes">Number of changes made.</param>
        /// <returns>A <see cref="BinTree"/> adjusted to the line width of the resolution.</returns>
        public static BinTree GetModdedBinTree(Stream entryStream, uint targetResolutionWidth, out int changes)
        {
            // Check whether a too small resolution was specified.
            if (targetResolutionWidth <= MAGIC_VALUE)
                throw new NotSupportedException($"A resolution width less than or equal to {(uint)MAGIC_VALUE} is not supported");

            // Define change counter
            changes = 0;

            // Read bin file
            var tree = new BinTree(entryStream);

            // go through all objects
            foreach (var obj in tree.Objects)
            {
                // Determine properties
                var mAnchors = obj?.GetPropertyByTyp<BinTreeStructure>(mAnchorsNameHash);
                var mRect = obj?.GetPropertyByTyp<BinTreeVector4>(mRectNameHash);
                var mRectSourceResolutionWidth = obj?.GetPropertyByTyp<BinTreeUInt16>(mRectSourceResolutionWidthNameHash);

                // Are not all properties present? => Skip object
                if (mRectSourceResolutionWidth == null || mRect == null || mAnchors == null)
                    continue;

                // Determine anchor
                var oldAnchor = mAnchors?.GetPropertyByTyp<BinTreeVector2>(AnchorNameHash);

                // Could not determine the anchor or is not supported? => Skip object
                if (oldAnchor == null || !IsAncorSupported(oldAnchor))
                    continue;

                // Arrange rectangle centrally according to the desired resolution width 
                var reanchored = ReanchorCentrally(mRect, oldAnchor, mRectSourceResolutionWidth, targetResolutionWidth);

                // Does the rectangle correspond to the output rectangle? => Skip object
                if (reanchored.Equals(mRect))
                    continue;

                // Assign centered rectangle and center anchor on X axis
                mRect.Value = reanchored;
                oldAnchor.Value = new Vector2((float)0.5, oldAnchor.Value.Y);

                // Increase change counter
                changes++;
            }

            // Return
            return tree;
        }

        #region Helper (privates)
        /// <summary>
        /// Determines a rectangle corresponding to a central anchoring for the target resolution width.
        /// </summary>
        /// <param name="oldRect">Source rectangle for the calculation.</param>
        /// <param name="oldAncor">Source anchor for the calculation.</param>
        /// <param name="sourceResolutionWidth">Width of the resolution on which the source rectangle is based.</param>
        /// <param name="targetResolutionWidth">Width of the resolution to be achieved.</param>
        /// <remarks>https://github.com/tnajdek/lol-eyefinity-surround-fixhud/blob/master/fixhud.py#L23</remarks>
        /// <returns>A <see cref="Vector4"/> adjusted to the line width of the resolution.</returns>
        private static Vector4 ReanchorCentrally(Vector4 oldRect, Vector2 oldAncor, uint sourceResolutionWidth, uint targetResolutionWidth)
        {
            // If the anchor for this element is not supported return original rect
            if (!IsAncorSupported(oldAncor))
                return oldRect;

            // Create new rect for the return
            var newRect = new Vector4(oldRect.X, oldRect.Y, oldRect.Z, oldRect.W);

            // Determine the ration for the desired resolution
            var ratio = targetResolutionWidth / MAGIC_VALUE;
            var offset = (ratio - 1.0) / 2.0;

            // Negate offset if the anchor is left
            if (oldAncor.X == 0)
                offset = Math.Abs(offset) * (-1);

            // depending on the anchor, adjust the X-positions for start and end accordingly
            newRect.X = (float)Math.Round(oldRect.X + (offset * sourceResolutionWidth), 1);
            newRect.Z = (float)Math.Round(oldRect.Z + (offset * sourceResolutionWidth), 1);

            // return new rect
            return newRect;
        }

        /// <summary>
        /// Checks if a <see cref="Vector2"/> is a supported anchor.
        /// </summary>
        /// <param name="ancor">Anchor to be checked</param>
        /// <returns>True if supported, False otherwise.</returns>
        private static bool IsAncorSupported(Vector2 ancor)
            => ancor.X == 1 || ancor.X == 0;
        #endregion
    }
}
