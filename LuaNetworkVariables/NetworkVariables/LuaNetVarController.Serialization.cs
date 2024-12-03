using ABI_RC.Systems.ModNetwork;
using MoonSharp.Interpreter;

namespace NAK.LuaNetVars;

public partial class LuaNetVarController
{
    private static DynValue DeserializeDynValue(ModNetworkMessage msg)
    {
        msg.Read(out byte dataTypeByte);
        DataType dataType = (DataType)dataTypeByte;

        switch (dataType)
        {
            case DataType.Boolean:
                msg.Read(out bool boolValue);
                return DynValue.NewBoolean(boolValue);
            case DataType.Number:
                msg.Read(out double numberValue);
                return DynValue.NewNumber(numberValue);
            case DataType.String:
                msg.Read(out string stringValue);
                return DynValue.NewString(stringValue);
            case DataType.Nil:
                return DynValue.Nil;
            default:
                LuaNetVarsMod.Logger.Error($"Unsupported data type received: {dataType}");
                return DynValue.Nil;
        }
    }

    private static void SerializeDynValue(ModNetworkMessage msg, DynValue value)
    {
        switch (value.Type)
        {
            case DataType.Boolean:
                msg.Write((byte)DataType.Boolean);
                msg.Write(value.Boolean);
                break;
            case DataType.Number:
                msg.Write((byte)DataType.Number);
                msg.Write(value.Number);
                break;
            case DataType.String:
                msg.Write((byte)DataType.String);
                msg.Write(value.String);
                break;
            case DataType.Nil:
                msg.Write((byte)DataType.Nil);
                break;
            default:
                LuaNetVarsMod.Logger.Error($"Unsupported DynValue type: {value.Type}");
                msg.Write((byte)DataType.Nil);
                break;
        }
    }

    private static bool IsSupportedDynValue(DynValue value)
    {
        return value.Type is DataType.Boolean or DataType.Number or DataType.String or DataType.Nil;
    }
}