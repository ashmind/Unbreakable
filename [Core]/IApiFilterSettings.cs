namespace Unbreakable {
    public interface IApiFilterSettings : IApiFilter {
        void SetupNamespace(string @namespace, ApiAccess access);
        void SetupType(string @namespace, string typeName, ApiAccess access);
        void SetupMember(string @namespace, string typeName, string memberName, ApiAccess access);
    }
}
