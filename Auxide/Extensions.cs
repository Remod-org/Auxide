using System.Reflection;
using System.Text;

namespace Auxide
{
    public static class StringExtension
    {
        public static string Titleize(this string s)
        {
            bool IsNewSentence = true;
            StringBuilder result = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (IsNewSentence && char.IsLetter(s[i]))
                {
                    result.Append(char.ToUpper(s[i]));
                    IsNewSentence = false;
                }
                else
                {
                    result.Append(s[i]);
                }

                if (s[i] == '!' || s[i] == '?' || s[i] == '.')
                {
                    IsNewSentence = true;
                }
            }

            return result.ToString();
        }
    }

    public class DroppedItemContainerExtension : DroppedItemContainer
    {
        public ItemContainer inventory
        {
            get
            {
                // Use reflection to get the value of the private inventory field in the base class
                FieldInfo inventoryFieldInfo = typeof(DroppedItemContainer).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);
                return (ItemContainer)inventoryFieldInfo.GetValue(this);
            }
            set
            {
                // Use reflection to set the value of the private inventory field in the base class
                FieldInfo inventoryFieldInfo = typeof(DroppedItemContainer).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);
                inventoryFieldInfo.SetValue(this, value);
            }
        }
    }

    public class BaseNetworkableExtension : BaseNetworkable
    {
        public bool _limitedNetworking
        {
            get
            {
                // Use reflection to get the value of the private inventory field in the base class
                FieldInfo limitedNetworkingFieldInfo = typeof(BaseNetworkable).GetField("_limitedNetworking", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BaseNetworkable)limitedNetworkingFieldInfo.GetValue(this);
            }
            set
            {
                // Use reflection to set the value of the private inventory field in the base class
                FieldInfo limitedNetworkingFieldInfo = typeof(BaseNetworkable).GetField("_limitedNetworking", BindingFlags.NonPublic | BindingFlags.Instance);
                limitedNetworkingFieldInfo.SetValue(this, value);
            }
        }
    }
    public class StorageContainerExtension : StorageContainer
    {
        public bool _limitedNetworking
        {
            get
            {
                // Use reflection to get the value of the private inventory field in the base class
                FieldInfo limitedNetworkingFieldInfo = typeof(BaseNetworkable).GetField("_limitedNetworking", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BaseNetworkable)limitedNetworkingFieldInfo.GetValue(this);
            }
            set
            {
                // Use reflection to set the value of the private inventory field in the base class
                FieldInfo limitedNetworkingFieldInfo = typeof(BaseNetworkable).GetField("_limitedNetworking", BindingFlags.NonPublic | BindingFlags.Instance);
                limitedNetworkingFieldInfo.SetValue(this, value);
            }
        }
    }
}