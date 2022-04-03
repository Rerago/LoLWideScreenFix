using LeagueToolkit.IO.PropertyBin;
using System.Linq;

namespace LoLWideScreenFix.Extensions
{
    /// <summary>
    /// Provides a collection of extensions for <see cref="BinTreeObject"/>.
    /// </summary>
    internal static class BinTreeObjectExtensions
    {
        /// <summary>
        /// Returns a property based on the type and the hash of the name.
        /// </summary>
        /// <typeparam name="T">Type of the property to be returned.</typeparam>
        /// <param name="obj">The <see cref="BinTreeObject"/> from which the property is to be determined.</param>
        /// <param name="hashName">Hash of the name of the property</param>
        /// <returns>The property of type <see cref="T"/>.</returns>
        internal static T GetPropertyByType<T>(this BinTreeObject obj, uint hashName) where T : BinTreeProperty
            => obj.Properties?.Where(x => x.NameHash == hashName)?.OfType<T>()?.FirstOrDefault();
    }
}
