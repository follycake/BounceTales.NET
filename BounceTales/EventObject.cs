using System.Diagnostics;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class EventObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 3;

    public enum State : byte
    {
        WAITING = 0,
        TERMINATED = 1,
        ACTIVE = 2,
        PAUSED = 3
    }

    // Global state
    //public static int FinalBossTimer = 0; //removed in 2.0.25
    public static int[] EventVars;

    // Global - stream pos after read function return
    private static int readPtr;

    private static EventObject[] currentEvents;
    private static GameObject[] triggerCandidates = new GameObject[2];

    // Parameters
    private byte[][] events;
    private byte eventCount;

    private byte triggerByLeave;
    private short triggerObjId;
    private byte repeatable;

    // State - interaction
    private GameObject[] actorsInAreaQueue = new GameObject[2];
    private int queuedAreaActorCount;
    private GameObject[] lastActorsInArea = new GameObject[2];
    private int lastAreaActorCount;

    private State eventState = 0;
    private sbyte currentEvent = -1;

    public override int ReadData(byte[] data, int dataPos)
    {
        dataPos = base.ReadData(data, dataPos);
        BBox.MinX = ReadShort(data, dataPos + 0) << 16;
        BBox.MaxY = ReadShort(data, dataPos + 2) << 16;
        BBox.MaxX = ReadShort(data, dataPos + 4) << 16;
        BBox.MinY = ReadShort(data, dataPos + 6) << 16;
        dataPos += 8;
        eventState = (State)data[dataPos++];
        if (eventState == State.ACTIVE)
        {
            Debug.WriteLine("Event " + GetObjectId() + " autostart!");
            currentEvent = 0;
        }
        triggerByLeave = data[dataPos++];
        repeatable = data[dataPos++];
        triggerObjId = ReadShort(data, dataPos);
        dataPos += 2;
        eventCount = data[dataPos++];
        events = new byte[eventCount][];
        for (int eventIdx = 0; eventIdx < eventCount; eventIdx++)
        {
            byte evCmdCount = data[dataPos++];
            events[eventIdx] = new byte[evCmdCount];
            for (int i = 0; i < evCmdCount; i++)
                events[eventIdx][i] = data[dataPos++];
        }
        if (!DEBUG_DRAW_ON)
            Flags |= ObjectFlags.NODRAW;
        return dataPos;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        DebugDraw(graphics, eventState switch
        {
            State.WAITING => 0x0000FF,
            State.TERMINATED => 0xFF0000,
            State.ACTIVE => 0x00FF00,
            State.PAUSED => 0x00CC00,
            _ => 0
        }, rootMatrix);
    }

    public void ChangeEventState(State newState)
    {
        switch (eventState)
        {
            case State.WAITING:
                if (newState == State.ACTIVE)
                {
                    eventState = State.ACTIVE;
                    currentEvent = 0;
                }
                if (newState == State.TERMINATED)
                    eventState = State.TERMINATED;
                break;
            case State.TERMINATED:
                if (newState == State.WAITING)
                {
                    eventState = State.WAITING;
                    ResetTransformEvents();
                }
                break;
            case State.ACTIVE:
                if (newState == State.TERMINATED || newState == State.PAUSED)
                    eventState = newState;
                break;
            case State.PAUSED:
                if (newState == State.ACTIVE)
                    eventState = State.ACTIVE;
                break;
        }
    }

    private static float GetFloatEventVar(byte b)
    {
        return BitConverter.Int32BitsToSingle(EventVars[b]);
    }

    private static void SetFloatEventVar(byte b, float f)
    {
        EventVars[b] = BitConverter.SingleToInt32Bits(f);
    }

    private static float ReadFloat(byte[] arr, int offset)
    {
        byte dataType = arr[offset];
        readPtr = offset + 1;
        switch (dataType)
        {
            case 1:
                readPtr++;
                return GetFloatEventVar(arr[offset + 1]);
            case 2:
                readPtr++;
                return EventVars[arr[offset + 1]];
            case 16:
                readPtr += 4;
                return BitConverter.Int32BitsToSingle(ReadInt(arr, offset + 1));
            case 32:
                readPtr += 4;
                return ReadInt(arr, offset + 1);
            default:
                return 0.0f;
        }
    }

    private static int ReadInteger(byte[] data, int offset)
    {
        byte size = data[offset];
        readPtr = 4;
        switch (size)
        {
            case 2:
                readPtr++;
                return EventVars[data[offset + 1]];
            case 32:
                readPtr += 4;
                return ReadInt(data, offset + 1);
            default:
                return 0;
        }
    }

    private static void Write16(byte[] bArr, int offset, int value)
    {
        // since 2.0.25, offset parameter assumed to be obfuscated
        bArr[offset] = (byte)(value >> 8);
        bArr[offset + 1] = (byte)value;
    }

    private static void Write32(byte[] bArr, int offset, int value)
    {
        bArr[offset] = (byte)(value >>> 24);
        bArr[offset + 1] = (byte)(value >> 16);
        bArr[offset + 2] = (byte)(value >> 8);
        bArr[offset + 3] = (byte)value;
    }

    private static void EventLog(string str)
    {
        Debug.WriteLine(str);
    }

    private bool ExecuteEvent(byte[] evCmd) // since 2.0.25
    {
        switch ((EventCommand)evCmd[0])
        {
            case EventCommand.MESSAGE:
                short msg = BounceGame.SCRIPT_MESSAGE_IDS[ReadShort(evCmd, 1)];
                if (!BounceGame.WasLevelBeaten(LevelID.GAME_CLEAR_LEVEL))
                    BounceGame.PushFieldMessage(msg);
                return true;
            case EventCommand.OBJ_ANIMATE: //s tart hit animation
                GameObject spriteGO = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (spriteGO != null)
                {
                    EventLog("Start sprite animation @ " + spriteGO.GetObjectId());
                    SpriteObject sprite = (SpriteObject)spriteGO;
                    sprite.OnPlayerContact();
                }
                return true;
            case EventCommand.EVENT_TERMINATE: // terminate event definitely
                currentEvents[evCmd[1]].ChangeEventState(State.TERMINATED);
                return true;
            case EventCommand.EVENT_CANCEL: // end event, but allow repeated execution
                currentEvents[evCmd[1]].ChangeEventState(State.WAITING);
                return true;
            case EventCommand.EVENT_START: // activate event
                EventLog("Activate event " + currentEvents[evCmd[1]].GetObjectId() + " (evId: " + evCmd[1] + ")");
                currentEvents[evCmd[1]].ChangeEventState(State.ACTIVE);
                return true;
            case EventCommand.EVENT_PAUSE:
                currentEvents[evCmd[1]].ChangeEventState(State.PAUSED);
                return true;
            case EventCommand.WAIT: // wait
                short waitTime = ReadShort(evCmd, 3);
                if (evCmd[3] == evCmd[1] && evCmd[4] == evCmd[2])
                    EventLog("Waiting for " + waitTime);
                if (waitTime < 0)
                {
                    evCmd[3] = evCmd[1];
                    evCmd[4] = evCmd[2];
                    return true;
                }
                Write16(evCmd, 3, waitTime - GameRuntime.UpdateDelta);
                return false;
            case EventCommand.VAR_SET: // set event variable
                if (evCmd[1] == 1)
                    SetFloatEventVar(evCmd[2], ReadFloat(evCmd, 3));
                if (evCmd[1] == 2)
                    EventVars[evCmd[2]] = ReadInteger(evCmd, 3);
                return true;
            case EventCommand.VAR_ADD: // event var +
                if (evCmd[1] == 1)
                    SetFloatEventVar(evCmd[2], GetFloatEventVar(evCmd[2]) + ReadFloat(evCmd, 3));
                if (evCmd[1] == 2)
                    EventVars[evCmd[2]] += ReadInteger(evCmd, 3);
                return true;
            case EventCommand.VAR_SUB: // event var -
                if (evCmd[1] == 1)
                    SetFloatEventVar(evCmd[2], GetFloatEventVar(evCmd[2]) - ReadFloat(evCmd, 3));
                if (evCmd[1] == 2)
                    EventVars[evCmd[2]] -= ReadInteger(evCmd, 3);
                return true;
            case EventCommand.VAR_MUL: // event var *
                if (evCmd[1] == 1)
                    SetFloatEventVar(evCmd[2], GetFloatEventVar(evCmd[2]) * ReadFloat(evCmd, 3));
                if (evCmd[1] == 2)
                    EventVars[evCmd[2]] *= ReadInteger(evCmd, 3);
                return true;
            case EventCommand.VAR_DIV: // event var /
                if (evCmd[1] == 1)
                    SetFloatEventVar(evCmd[2], GetFloatEventVar(evCmd[2]) / ReadFloat(evCmd, 3));
                if (evCmd[1] == 2)
                    EventVars[evCmd[2]] /= ReadInteger(evCmd, 3);
                return true;
            case EventCommand.BRANCH_IF_NE: // event var CMPEQ
                if (evCmd[1] == 2)
                {
                    if (EventVars[evCmd[2]] == ReadInteger(evCmd, 3))
                        return true;
                }
                else if (evCmd[1] == 1 && GetFloatEventVar(evCmd[2]) == ReadFloat(evCmd, 3))
                    return true;
                currentEvent = (sbyte)(evCmd[readPtr] - 2);
                return true;
            case EventCommand.BRANCH_IF_EQ: // event var CMPNE
                if (evCmd[1] == 2)
                {
                    if (EventVars[evCmd[2]] != ReadInteger(evCmd, 3))
                        return true;
                }
                else if (evCmd[1] == 1 && GetFloatEventVar(evCmd[2]) != ReadFloat(evCmd, 3))
                    return true;
                currentEvent = (sbyte)(evCmd[readPtr] - 2);
                return true;
            case EventCommand.BRANCH_IF_GEQ: // event var CMPLESS
                if (evCmd[1] == 2)
                {
                    if (EventVars[evCmd[2]] < ReadInteger(evCmd, 3))
                        return true;
                }
                else if (evCmd[1] == 1 && GetFloatEventVar(evCmd[2]) < ReadFloat(evCmd, 3))
                    return true;
                currentEvent = (sbyte)(evCmd[readPtr] - 2);
                return true;
            case EventCommand.BRANCH_IF_LEQ: // event var CMPGRTR
                if (evCmd[1] == 2)
                {
                    if (EventVars[evCmd[2]] > ReadInteger(evCmd, 3))
                        return true;
                }
                else if (evCmd[1] == 1 && GetFloatEventVar(evCmd[2]) > ReadFloat(evCmd, 3))
                    return true;
                currentEvent = (sbyte)(evCmd[readPtr] - 2);
                return true;
            case EventCommand.OBJ_MOVE: // move object to position
                GameObject moveobj = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (moveobj == null)
                    return true;
                {
                    int counter = ReadInt(evCmd, 15);
                    int delta = GameRuntime.UpdateDelta;
                    if (delta >= counter)
                    {
                        evCmd[15] = evCmd[11];
                        evCmd[16] = evCmd[12];
                        evCmd[17] = evCmd[13];
                        evCmd[18] = evCmd[14];
                        delta = counter;
                    }
                    moveobj.LocalObjectMatrix.TranslationX += ReadInt(evCmd, 3) * delta;
                    moveobj.LocalObjectMatrix.TranslationY += ReadInt(evCmd, 7) * delta;
                    moveobj.SetIsDirtyRecursive();
                    moveobj.SetBBoxIsDirty();
                    int newCounter = counter - delta;
                    if (newCounter <= 0)
                        return true;
                    Write32(evCmd, 15, newCounter);
                    return false;
                }
            case EventCommand.OBJ_ROTATE: // rotate object
                GameObject rotObj = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (rotObj == null)
                    return true;
                {
                    int counter = ReadInt(evCmd, 11);
                    int duration = ReadInt(evCmd, 7);
                    int delta = GameRuntime.UpdateDelta;
                    if (counter == duration)
                    {
                        Write32(evCmd, 15, rotObj.LocalObjectMatrix.M00); // save initial rotation
                        Write32(evCmd, 19, rotObj.LocalObjectMatrix.M01);
                        Write32(evCmd, 23, rotObj.LocalObjectMatrix.M10);
                        Write32(evCmd, 27, rotObj.LocalObjectMatrix.M11);
                    }
                    if (counter - delta <= 0)
                    {
                        evCmd[11] = evCmd[7];
                        evCmd[12] = evCmd[8];
                        evCmd[13] = evCmd[9];
                        evCmd[14] = evCmd[10];
                        counter = 0;
                    }
                    float rotation = LP32.LP32ToFP32((int)(ReadInt(evCmd, 3) * (long)(duration - counter) / duration));
                    Matrix temp = Matrix.Identity;
                    temp.SetRotation(rotation);
                    temp.TranslationX = 0;
                    temp.TranslationY = 0;
                    rotObj.LocalObjectMatrix.M00 = ReadInt(evCmd, 15);
                    rotObj.LocalObjectMatrix.M01 = ReadInt(evCmd, 19);
                    rotObj.LocalObjectMatrix.M10 = ReadInt(evCmd, 23);
                    rotObj.LocalObjectMatrix.M11 = ReadInt(evCmd, 27);
                    rotObj.LocalObjectMatrix.Mul(temp);
                    rotObj.SetIsDirtyRecursive();
                    rotObj.SetBBoxIsDirty();
                    int newCounter = counter - delta;
                    if (newCounter <= 0)
                        return true;
                    Write32(evCmd, 11, newCounter);
                    return false;
                }
            case EventCommand.OBJ_SETPOS: // copy translation from other object or eventcmd
                GameObject destObj = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (destObj != null)
                {
                    short srcObjId = ReadShort(evCmd, 3);
                    if (srcObjId < 0)
                    {
                        destObj.LocalObjectMatrix.TranslationX = ReadInt(evCmd, 5);
                        destObj.LocalObjectMatrix.TranslationY = ReadInt(evCmd, 9);
                        EventLog("Set translation of object " + destObj.GetObjectId() + " to (" + (destObj.LocalObjectMatrix.TranslationX >> 16) + ", " + (destObj.LocalObjectMatrix.TranslationY >> 16) + ")");
                    }
                    else
                    {
                        EventLog("Set translation of object " + destObj.GetObjectId() + " from " + srcObjId);
                        GameObject srcObj = GetObjectRoot().SearchByObjId(srcObjId);
                        if (srcObj != null)
                        {
                            srcObj.LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
                            Vector2I srcTAbs = tmpObjMatrix.Translation;
                            destObj.LocalObjectMatrix.Translation = Vector2I.Zero;
                            destObj.ObjectMatrixIsDirty = true;
                            destObj.LoadObjectMatrixToTarget(out tmpObjMatrix);
                            tmpObjMatrix.Invert(out Matrix temp); // load inverse rotation matrix
                            destObj.LocalObjectMatrix.Translation = temp.MulVector(srcTAbs);
                        }
                    }
                    destObj.SetIsDirtyRecursive();
                    destObj.SetBBoxIsDirty();
                    destObj.LoadObjectMatrixToTarget(out destObj.RenderCalcMatrix);
                }
                return true;
            case EventCommand.OBJ_ATTACH: // reset parent to level root
                {
                    short parentWhoId = ReadShort(evCmd, 1);
                    short parentToId = ReadShort(evCmd, 3);
                    GameObject parentWho = dummyParent.SearchByObjId(parentWhoId);
                    if (parentWho != null)
                    {
                        GameObject parentTo = GetObjectRoot().SearchByObjId(parentToId);
                        if (parentTo != null)
                        {
                            parentWho.SetParent(parentTo);
                            parentWho.SetBBoxIsDirty();
                        }
                    }
                    return true;
                }
            case EventCommand.OBJ_DETACH: // parent to dummy
                {
                    GameObject parentWho = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                    if (parentWho != null)
                    {
                        parentWho.SetBBoxIsDirty();
                        parentWho.SetParent(dummyParent);
                    }
                    return true;
                }
            case EventCommand.BRANCH: // branch to event unconditionally
                currentEvent = (sbyte)(evCmd[1] - 2);
                EventLog("BranchEvent " + currentEvent);
                return true;
            case EventCommand.NOP: // NOP
                return true;
            case EventCommand.END: // end event
                eventState = repeatable == 1 ? State.WAITING : State.TERMINATED;
                return true;
            case EventCommand.WAIT_ACTOR_GONE:
                return !ArrayContains(lastActorsInArea, lastAreaActorCount, GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1)));
            case EventCommand.CHECKPOINT: // checkpoint reached
                BounceGame.CheckpointPosX = RenderCalcMatrix.TranslationX;
                BounceGame.CheckpointPosY = RenderCalcMatrix.TranslationY;
                EventLog("Checkpoint reached: " + BounceGame.CheckpointPosX + ", " + BounceGame.CheckpointPosY);
                return true;
            case EventCommand.PUSH: // force gravity push
                GameObject pushTarget = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (pushTarget.GetObjType() == BounceObject.TYPEID)
                {
                    BounceObject bounce = (BounceObject)pushTarget;
                    bounce.PushX += ReadShort(evCmd, 3);
                    bounce.PushY += ReadShort(evCmd, 5);
                }
                return true;
            case EventCommand.GRAVITATE:
                GameObject gravityTarget = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (gravityTarget.GetObjType() == BounceObject.TYPEID)
                {
                    BounceObject bounce = (BounceObject)gravityTarget;
                    bounce.GravityX += ReadShort(evCmd, 3);
                    bounce.GravityY += ReadShort(evCmd, 5);
                }
                return true;
            case EventCommand.ACCELERATE:
                GameObject accelTarget = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (accelTarget.GetObjType() == BounceObject.TYPEID)
                {
                    BounceObject bounce = (BounceObject)accelTarget;
                    bounce.CurXVelocity += ReadShort(evCmd, 3);
                    bounce.CurYVelocity += ReadShort(evCmd, 5);
                }
                return true;
            case EventCommand.OBJ_SET_FLAGS:
                GameObject obj = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                if (obj != null)
                {
                    EventLog("evcmd 29 on object " + obj.GetObjectId());
                    ObjectFlags flagExistMask = (ObjectFlags)ReadInt(evCmd, 3);
                    ObjectFlags flagValues = (ObjectFlags)ReadInt(evCmd, 7);
                    if ((flagExistMask & (ObjectFlags)1) != 0)
                    {
                        obj.Flags &= ~ObjectFlags.Z_COORD_MASK;
                        obj.Flags |= flagValues & ObjectFlags.Z_COORD_MASK;
                        obj.ZCoord = (sbyte)((obj.Flags & ObjectFlags.Z_COORD_MASK) - 16);
                    }
                    if ((flagExistMask & ObjectFlags.NOCOLLIDE) != 0)
                    {
                        obj.Flags &= ~ObjectFlags.NOCOLLIDE;
                        obj.Flags |= flagValues & ObjectFlags.NOCOLLIDE;
                    }
                    if ((flagExistMask & ObjectFlags.NODRAW) != 0)
                    {
                        obj.Flags &= ~ObjectFlags.NODRAW;
                        obj.Flags |= flagValues & ObjectFlags.NODRAW;
                        if (obj.GetObjType() == SpriteObject.TYPEID)
                        {
                            SpriteObject sprite = (SpriteObject)obj;
                            if (sprite.imageIDs[0] == 358) // evil machine
                            {
                                sprite.LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
                                BounceGame.ColorMachineDestroyParticle.EmitIndependentBursts(
                                    24,
                                    sprite.BBox.MinX + (60 << 16) + tmpObjMatrix.TranslationX,
                                    sprite.BBox.MinY + (60 << 16) + tmpObjMatrix.TranslationY,
                                    sprite.BBox.MaxX - (60 << 16) + tmpObjMatrix.TranslationX,
                                    sprite.BBox.MaxY - (60 << 16) + tmpObjMatrix.TranslationY,
                                    840,
                                    0,
                                    0,
                                    360,
                                    2040,
                                    510
                                );
                            }
                        }
                    }
                    if ((flagExistMask & ObjectFlags.UNKNOWN) != 0)
                    {
                        obj.Flags &= ~ObjectFlags.UNKNOWN;
                        obj.Flags |= flagValues & ObjectFlags.UNKNOWN;
                        EventLog("Unknown flag (256) set for object: " + obj);
                    }
                }
                return true;
            case EventCommand.CAMERA_TARGET: // change camera target
                CameraTarget = GetObjectRoot().SearchByObjId(ReadShort(evCmd, 1));
                EventLog("New camera target: " + CameraTarget);
                return true;
            case EventCommand.CAMERA_SETPARAM:
                BounceGame.ReqCameraSnap = evCmd[1] == 1;
                CameraBounceFactor = ReadShort(evCmd, 2);
                CameraStabilizeSpeed = ReadShort(evCmd, 4);
                EventLog("Camera return: snap " + BounceGame.ReqCameraSnap + " / a " + CameraBounceFactor + " / b " + CameraStabilizeSpeed);
                return true;
            case EventCommand.CAMERA_SETPARAM_DEFAULT:
                BounceGame.ReqCameraSnap = false;
                CameraBounceFactor = 90;
                CameraStabilizeSpeed = 140;
                return true;
            default:
                return true;
        }
    }

    public static void UpdateEvents(EventObject[] events)
    {
        currentEvents = events;
        foreach (EventObject eventObj in events)
        {
            if (eventObj.triggerByLeave == 0)
            {
                for (int i = 0; i < eventObj.queuedAreaActorCount; i++)
                {
                    GameObject queueActor = eventObj.actorsInAreaQueue[i];
                    if (!ArrayContains(eventObj.lastActorsInArea, eventObj.lastAreaActorCount, queueActor))
                        triggerCandidates[i] = queueActor;
                }
            }
            else
            {
                for (int i = 0; i < eventObj.lastAreaActorCount; i++)
                {
                    GameObject curActor = eventObj.lastActorsInArea[i];
                    if (!ArrayContains(eventObj.actorsInAreaQueue, eventObj.queuedAreaActorCount, curActor))
                        triggerCandidates[i] = curActor;
                }
            }
            foreach (GameObject triggerCandidate in triggerCandidates)
            {
                if (triggerCandidate != null && (eventObj.triggerObjId <= -1 || triggerCandidate.GetObjectId() == eventObj.triggerObjId))
                {
                    Debug.WriteLine("Actor " + triggerCandidate.GetObjectId() + " triggered event " + eventObj.GetObjectId());
                    eventObj.ChangeEventState(State.ACTIVE);
                    break;
                }
            }
            for (int i = 0; i < eventObj.actorsInAreaQueue.Length; i++)
            {
                eventObj.lastActorsInArea[i] = eventObj.actorsInAreaQueue[i];
                eventObj.actorsInAreaQueue[i] = null;
                triggerCandidates[i] = null;
            }
            eventObj.lastAreaActorCount = eventObj.queuedAreaActorCount;
            eventObj.queuedAreaActorCount = 0;
        }
        foreach (EventObject eventObj in events)
        {
            while (eventObj.IsChildOf(BounceGame.RootLevelObj) && eventObj.eventState == State.ACTIVE && eventObj.ExecuteEvent(eventObj.events[eventObj.currentEvent]))
            {
                /*if (BounceGame.currentLevel == LevelID.FINAL_RIDE) {
                    finalBossTimer += GameRuntime.updateDelta;
                    //kinda bug, kinda snack. this probably shouldn't be getting updated with each eventObj.
                    if (eventObj.objectId == 15) {
                        if (finalBossTimer <= 20000) {
                            eventObj.eventState = STATE_WAITING;
                        }
                    }
                }*/ //removed in 2.0.25
                if (eventObj.currentEvent < -1)
                    eventObj.currentEvent = -1;
                eventObj.currentEvent++;
                if (eventObj.currentEvent >= eventObj.eventCount)
                {
                    if (eventObj.repeatable == 1)
                    {
                        eventObj.eventState = State.WAITING;
                        eventObj.ResetTransformEvents();
                    }
                    else
                        eventObj.eventState = State.TERMINATED;
                }
            }
        }
    }

    private void ResetTransformEvents()
    {
        currentEvent = -1;
        foreach (byte[] evt in events)
        {
            switch (evt[0])
            {
                case 6:
                    evt[3] = evt[1];
                    evt[4] = evt[2];
                    break;
                case 16:
                    evt[15] = evt[11];
                    evt[16] = evt[12];
                    evt[17] = evt[13];
                    evt[18] = evt[14];
                    break;
                case 17:
                    evt[11] = evt[7];
                    evt[12] = evt[8];
                    evt[13] = evt[9];
                    evt[14] = evt[10];
                    break;
            }
        }
    }

    public static void CheckBounceEventTrigger(EventObject[] events, BounceObject bounce)
    {
        int bboxW = bounce.BBox.MaxX - bounce.BBox.MinX;
        int bboxH = bounce.BBox.MaxY - bounce.BBox.MinY;
        int maxBBoxDimHalf = bboxW < bboxH ? bboxH >> 1 : bboxW >> 1;
        foreach (EventObject eventObj in events)
        {
            if (eventObj.eventState != State.TERMINATED)
            {
                eventObj.LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
                tmpObjMatrix.Invert(out Matrix temp);
                Vector2I bounceOld = temp.MulVector(bounce.RenderCalcMatrix.Translation);
                Vector2I bounceNew = temp.MulVector(bounce.LocalObjectMatrix.Translation);
                AABB aabb = new(
                    eventObj.BBox.MinX - maxBBoxDimHalf,
                    eventObj.BBox.MinY - maxBBoxDimHalf,
                    eventObj.BBox.MaxX + maxBBoxDimHalf,
                    eventObj.BBox.MaxY + maxBBoxDimHalf
                );
                if ((aabb.CheckBoundCross(bounceOld, bounceNew) || aabb.ContainsPoint(bounceNew)) && eventObj.queuedAreaActorCount < 2)
                {
                    eventObj.actorsInAreaQueue[eventObj.queuedAreaActorCount] = bounce;
                    eventObj.queuedAreaActorCount++;
                }
            }
        }
    }

    private static bool ArrayContains(GameObject[] array, int count, GameObject obj)
    {
        for (int i = 0; i < count; i++)
        {
            if (array[i] == obj)
                return true;
        }
        return false;
    }
}
