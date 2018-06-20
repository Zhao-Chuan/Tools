using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;


namespace SharedTools_Stuff
{

    public interface iCountlessIndex
    {
        int CountlessIndex { get; set; }
    }


    public abstract class CountlessBase {

        protected static VariableBranch[] branchPool = new VariableBranch[32];
        protected static VariableBranch[] fruitPool = new VariableBranch[32];
        protected static int brPoolMax = 0;
        protected static int frPoolMax = 0;
        protected static ArrayManager<VariableBranch> array = new ArrayManager<VariableBranch>();
        protected static int branchSize = 8;
        protected static void DiscardFruit(VariableBranch b, int no)
        {
            if ((frPoolMax + 1) >= fruitPool.Length)
                array.Expand(ref fruitPool, 32);

            fruitPool[frPoolMax] = b.br[no];
            VariableBranch vb = fruitPool[frPoolMax];
            vb.value = 0;
            b.br[no] = null;
            b.value--;
            frPoolMax++;
        }
        protected static void DiscardBranch(VariableBranch b, int no)
        {
            if ((brPoolMax + 1) >= branchPool.Length)
            {
                array.Expand(ref branchPool, 32);
            }
            //Debug.Log("Deleting branch ");
            branchPool[brPoolMax] = b.br[no];
            VariableBranch vb = branchPool[brPoolMax];
            if (vb.value != 0)
                Debug.Log("Value is " + vb.value + " on delition ");
            //vb.value = 0;
            b.value--;
            b.br[no] = null;
            brPoolMax++;
        }
        protected void TryReduceDepth()
        {
            while ((br.value < 2) && (br.br[0] != null) && (depth > 0))
            {
                // if (br.value < 1) Debug.Log("Reducing depth on branch with " + br.value);
                branchPool[brPoolMax] = br;
                brPoolMax++;
                VariableBranch tmp = br.br[0];
                br.br[0] = null;
                br.value = 0;
                br = tmp;
                depth--;
                Max /= branchSize;

                // Debug.Log("Reducing depth to " + depth + " new Range: " + Max);
            }
        }
        protected static void DiscardCascade(VariableBranch b, int depth)
        {
            if ((brPoolMax + 1) >= branchPool.Length)
            {
                array.Expand(ref branchPool, 32);
            }

            if (depth > 0)
            {
                for (int i = 0; i < branchSize; i++)
                {
                    if (b.br[i] != null)
                    {
                        DiscardCascade(b.br[i], depth - 1);
                        DiscardBranch(b, i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                        DiscardFruit(b, i);
            }

        }
        protected static VariableBranch getNewBranch()
        {
            if (brPoolMax == 0)
            {
                VariableBranch vb = new VariableBranch();
                vb.br = new VariableBranch[branchSize];
                //Debug.Log("Creating new branch ");
                return vb;
            }
            brPoolMax--;
            //Debug.Log("Returning existing branch");
            return branchPool[brPoolMax];
        }
        protected static VariableBranch getNewFruit()
        {

            if (frPoolMax == 0)
            {
                VariableBranch vb = new VariableBranch();
                //   Debug.Log("Creating new fruit ");
                return vb;
            }
            frPoolMax--;
            // Debug.Log("Returning existing fruit");
            return fruitPool[frPoolMax];
        }

        public virtual void Clear()
        {
            DiscardCascade(br, depth);
        }

        protected int firstFree;
        protected int depth;
        protected int Max;
        protected VariableBranch[] path;
        protected int[] pathInd;
        protected VariableBranch br;
        protected int lastFreeIndex;

        #if PEGI

        public virtual bool PEGI()
        {
            ("Depth: " + depth).nl();
            ("First free: " + firstFree).nl();

            return false;
        }
#endif

        public CountlessBase()
        {
            Max = branchSize;
            depth = 0;
            br = getNewBranch();
        }

        public delegate void VariableTreeFunk(ref int dst, int ind, int val);
    }


    public abstract class STDCountlessBase : CountlessBase, iSTD
    {

        public virtual stdEncoder Encode() {
            return null; }

        public virtual iSTD Decode(string data)
        {
            Clear();
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public virtual bool Decode(string subtag, string data) { return true; }


    }

    public class CountlessInt : STDCountlessBase
    {

        List<int> inds;

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "inds": data.DecodeInto(out inds); break;
                case "vals":
                    List<int> vals; data.DecodeInto(out vals);
                    for (int i = 0; i < vals.Count; i++)
                        Set(inds[i], vals[i]);
                    inds = null;
                    break;
                case "last": lastFreeIndex = data.ToInt(); break;
                default: return false;
            }
            return true;

        }

        public override stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            List<int> vals;

            GetItAll(out inds, out vals);

            cody.Add_IfNotEmpty("inds", inds);
            cody.Add_IfNotEmpty("vals", vals);
            cody.Add("last", lastFreeIndex);


            inds = null;

            return cody;
        }

     //   public const string storyTag = "TreeInt";
       // public override string getDefaultTagName() { return storyTag; }

        public void GetItAll(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetItAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }
        void GetItAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        public int this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        int Get(int ind)
        {
            if (ind >= Max)
                return 0;

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return 0;

                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return 0;

            return vb.br[ind].value;
        }

        void Set(int ind, int val)
        {

            //Debug.Log("Setting "+ind+" to "+val);

            if (ind >= Max)
            {
                if (val == 0)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (val != 0)
            {
                while (d > 0)
                {

                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = getNewFruit();
                    vb.value += 1;
                }


                vb.br[ind].value = val;
            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        public void Add(int ind, int val)
        {

            if (ind >= Max)
            {
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;


            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
            {
                vb.br[ind] = getNewFruit();
                vb.value += 1;
            }

            vb.br[ind].value += val;

        }

    }
    
    public class CountlessBool : STDCountlessBase
    {

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "inds":
                    List<int> inds; data.DecodeInto(out inds);
                    foreach (int i in inds)
                        Set(i, true);

                    inds = null;
                    break;
                case "last": lastFreeIndex = data.ToInt(); break;
                default: return false;
            }
            return true;

        }

        public override stdEncoder Encode() => new stdEncoder().Add_IfNotEmpty("inds", GetItAll()).Add("last", lastFreeIndex);

      //  public const string storyTag = "TreeBool";
      //  public override string getDefaultTagName() =>  storyTag; 

        public List<int> GetItAll()
        {
            List<int> inds = new List<int>();
            GetItAllCascadeBool(ref inds, br, depth, 0, Max);
            return inds;
        }

        void GetItAllCascadeBool(ref List<int> inds, VariableBranch b, int dp, int start, int range)
        {

            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeBool(ref inds, b.br[i], dp - 1, start + step * i, step);
            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        int value = b.br[i].value;

                        for (int j = 0; j < 32; j++)
                            if ((value & (int)(0x00000001 << j)) != 0)
                                //{
                                inds.Add((start + i) * 32 + j);
                        //  vals.Add(1);
                        // }
                    }
            }
        }

        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        bool Get(int ind)
        {

#if UNITY_EDITOR
            if (ind < 0) Debug.LogError("Sending " + ind + " as index to Variable Tree, that is a nono");
#endif

            int bitNo = ind % 32;
            ind /= 32;
            if (ind >= Max)
                return false;

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return false;
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return false;

            VariableBranch fvb = vb.br[ind];

            return ((fvb.value & (int)(0x00000001 << bitNo)) != 0);
            //return Get(ind) > 0;
        }

        void Set(int ind, bool val)
        {
            int bitNo = ind % 32;
            ind /= 32;

            if (ind >= Max)
            {
                if (!val)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;


            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                d--;
                path[d] = vb;
                pathInd[d] = no;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
            {
                vb.br[ind] = getNewFruit();
                vb.value += 1;
            }

            VariableBranch fvb = vb.br[ind];
            if (val)
                fvb.value |= (int)(0x00000001 << bitNo);
            else
                fvb.value &= (int)(~(0x00000001 << bitNo));
            //vb.br[ind].value = val;

            if (fvb.value == 0)
                DiscardFruit(vb, ind);

            while (d < depth)
            {
                if (vb.value > 0)
                    return;
                vb = path[d];
                DiscardBranch(vb, pathInd[d]);
                d++;
            }

            TryReduceDepth();
        }

        public bool Toggle(int ind)
        {



            int bitNo = ind % 32;
            ind /= 32;

            if (ind >= Max)
            {
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;


            while (d > 0)
            {

                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                d--;
                path[d] = vb;
                pathInd[d] = no;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
            {

                vb.br[ind] = getNewFruit();
                vb.value += 1;
            }

            VariableBranch fvb = vb.br[ind];
            // bitFunk(ref fvb.value, bitNo, val);
            fvb.value ^= (int)(0x00000001 << bitNo);
            //vb.br[ind].value = val;

            bool rslt = (((fvb.value & (int)(0x00000001 << bitNo)) != 0) ? true : false);

            if (fvb.value == 0)
                DiscardFruit(vb, ind);



            while (d < depth)
            {
                if (vb.value > 0)
                    return rslt;
                vb = path[d];
                DiscardBranch(vb, pathInd[d]);
                d++;
            }

            TryReduceDepth();

            return rslt;
        }

      

    }
    
    ///  Generic Trees
    public class Countless<T> : CountlessBase //, IEnumerable
    {
        
        T[] objs = new T[0];
        int firstFreeObj = 0;

        public void Expand(ref T[] args, int add)  {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
        }

        public T this[int index]
        {
            get { return Get(index); }
            set {
#if PEGI
                var igi = value as iGotIndex;
                if (igi != null && igi.GetIndex() != index)
                {
                    Debug.Log("setting "+value.ToString() + " with ind " + igi.GetIndex() + " at "+index);
                 //   igi.index = index;
                }
#endif
                Set(index, value); }
        }

#if PEGI
        public T this[iGotIndex i]
        {
            get { return Get(i.GetIndex()); }
            set { Set(i.GetIndex(), value); }
        }
#endif
        void Set(int ind, T obj)
        {
            
            if (ind >= Max)
            {
                if (obj.isDefaultOrNull())
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (!obj.isDefaultOrNull())
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = getNewFruit();
                    vb.value += 1;

                    int cnt = objs.Length;
                    while ((firstFreeObj < cnt) && (!objs[firstFreeObj].isDefaultOrNull())) firstFreeObj++;
                    if (firstFreeObj >= cnt)
                        Expand(ref objs, branchSize);

                    objs[firstFreeObj] = obj;
                    vb.br[ind].value = firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                int ar = vb.br[ind].value;

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

                objs[ar] = default(T);
                firstFreeObj = Mathf.Min(firstFreeObj, ar);

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        T Get(int ind)
        {
            if (ind >= Max)
                return default(T);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return default(T);

            return objs[vb.br[ind].value];
        }

        public List<T> GetAllObjsNoOrder()
        {
            List<T> tmp = new List<T>();
            for (int i = 0; i < objs.Length; i++)
                if (!objs[i].isDefaultOrNull())
                    tmp.Add(objs[i]);

            return tmp;
        }

        public List<T> GetAllObjs(out List<int> inds)
        {
            List<T> objects = new List<T>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }
        
        public override void Clear()
        {
            base.Clear();
            objs = new T[0];
            firstFreeObj = 0;
        }

        public virtual bool NotEmpty => objs.Length > 0;

        public string getDefaultTagName()
        {
            return "cntless";
        }

        public IEnumerator<T> GetEnumerator() {
            List<int> indx;
            List<T> all = GetAllObjs(out indx);
            for (int i = 0; i < all.Count; i++) {
                var e = all[i];
                if (!e.isDefaultOrNull()) {
                    currentEnumerationIndex = indx[i];
                    yield return e;
                }
            }
        }

        public int currentEnumerationIndex;
#if PEGI
        public override bool PEGI() {
            base.PEGI();
            foreach (var o in objs)
                if (o == null) "null".nl();
                else
                    o.ToString().nl();

            return false;
        }
#endif

    }

    public class CountlessSTD<T> : STDCountlessBase, IEnumerable where T : iSTD , new() {

        protected T[] objs = new T[0];
        int firstFreeObj = 0;

        public override bool Decode(string tag, string data)
        {
            T tmp;
            int ind = tag.ToIntFromText();
           // Debug.Log(ind + " decodding "+data);
            this[ind] = data.DecodeInto(out tmp);
           
         
            return true;
        }

        public override stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            List<int> inds;
            List<T> vals = GetAllObjs(out inds);

            for (int i = 0; i < inds.Count; i++) {
                var dta = vals[i].Encode().ToString();
                //Debug.Log(inds[i] + " Encodingg " + dta);
                cody.Add_String(inds[i].ToString(), dta);
            }

            return cody;
        }

     //   public const string storyTag = "TreeObj";
      //  public override string getDefaultTagName() { return storyTag; }

        public void Expand(ref T[] args, int add) // no instantiating
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
            // for (int i = args.Length - add; i < args.Length; i++)
            //   args[i] = new T();
        }

        public T this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        protected void Set(int ind, T obj)
        {
            if (ind >= Max)
            {
                if (obj == null)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (obj != null)
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = getNewFruit();
                    vb.value += 1;

                    int cnt = objs.Length;
                    while ((firstFreeObj < cnt) && (objs[firstFreeObj] != null)) firstFreeObj++;
                    if (firstFreeObj >= cnt)
                        Expand(ref objs, branchSize);

                    objs[firstFreeObj] = obj;
                    vb.br[ind].value = firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                int ar = vb.br[ind].value;

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

                objs[ar] = default(T);
                firstFreeObj = Mathf.Min(firstFreeObj, ar);

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        public List<T> GetAllObjsNoOrder()
        {
            List<T> tmp = new List<T>();
            for (int i = 0; i < objs.Length; i++)
                if (objs[i] != null)
                    tmp.Add(objs[i]);

            return tmp;
        }

        public List<T> GetAllObjs(out List<int> inds)
        {
            List<T> objects = new List<T>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        protected virtual T Get(int ind)
        {
            if (ind >= Max)
                return default(T);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return default(T);

            return objs[vb.br[ind].value];
        }

        public override void Clear()
        {
            base.Clear();
            objs = new T[0];
            firstFreeObj = 0;
        }

        public IEnumerator GetEnumerator()
        {
            List<int> indx;
            var all = GetAllObjs(out indx);
            for (int i = 0; i < all.Count; i++)
            {
               

                var e = all[i];

                //Debug.Log("Enum " + indx[i] + " " +e.ToString());

                if (!e.isDefaultOrNull())
                {
                    currentEnumerationIndex = indx[i];
                    yield return e;
                }
            }
        }

        public int currentEnumerationIndex;
        #if PEGI
        public override bool PEGI()
        {
            bool changed = false;



            return changed;
        }
#endif
    }

    // Unnulable classes will create new instances
    public class UnnullableSTD<T> : CountlessSTD<T> where T : iSTD, new()  {

        public static int IndexOfCurrentlyCreatedUnnulable;

        T Create(int ind) {
            IndexOfCurrentlyCreatedUnnulable = ind;
            T tmp = new T();
            Set(ind, tmp);
            return tmp;
        }

        public int AddNew()
        {
            IndexOfCurrentlyCreatedUnnulable = -1;

            while (IndexOfCurrentlyCreatedUnnulable == -1)
            {
                Get(firstFree);
                firstFree++;
            }

            return IndexOfCurrentlyCreatedUnnulable;
        }

        protected override T Get(int ind)  {
            int originalIndex = ind;

            if (ind >= Max)
                return Create(originalIndex);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return Create(originalIndex);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return Create(originalIndex);

            return objs[vb.br[ind].value];
        }

        public T GetIfElementExists(int ind)
        {
            // int originalIndex = ind;

            if (ind >= Max)
                return default(T);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return default(T);

            return objs[vb.br[ind].value];
        }

    }

    // List trees
    public class UnnullableLists<T> : STDCountlessBase, IEnumerable {

        List<T>[] objs = new List<T>[0];
        int firstFreeObj = 0;

        public void Expand(ref List<T>[] args, int add) // no instantiating
        {
            List<T>[] temp;
            if (args != null)
            {
                temp = new List<T>[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new List<T>[add];
            args = temp;
            // for (int i = args.Length - add; i < args.Length; i++)
            //   args[i] = new T();
        }

        public List<T> this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        void Set(int ind, List<T> obj)
        {
            if (ind >= Max)
            {
                if (obj == null)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = getNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (obj != null)
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = getNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = getNewFruit();
                    vb.value += 1;

                    int cnt = objs.Length;
                    while ((firstFreeObj < cnt) && (objs[firstFreeObj] != null)) firstFreeObj++;
                    if (firstFreeObj >= cnt)
                        Expand(ref objs, branchSize);

                    objs[firstFreeObj] = obj;
                    vb.br[ind].value = firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                int ar = vb.br[ind].value;

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

                objs[ar] = default(List<T>);
                firstFreeObj = Mathf.Min(firstFreeObj, ar);

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        public List<List<T>> GetAllObjsNoOrder()
        {
            List<List<T>> tmp = new List<List<T>>();
            for (int i = 0; i < objs.Length; i++)
                if (objs[i] != null)
                    tmp.Add(objs[i]);

            return tmp;
        }

        public List<List<T>> GetAllObjs(out List<int> inds)
        {
            List<List<T>> objects = new List<List<T>>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        List<T> Create(int ind)
        {
            if (ind < 0)
            {
                Debug.Log("!Wrong index");
                return null;
            }
            var tmp = new List<T>();
            this[ind] = tmp;
            return tmp;
        }

        List<T> Get(int ind)
        {
            int originalIndex = ind;

            if (ind >= Max)
                return Create(originalIndex);//default(List<T>);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return Create(originalIndex);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return Create(originalIndex);

            return objs[vb.br[ind].value];
        }

        public override void Clear()
        {
            base.Clear();
            objs = new List<T>[0];
            firstFreeObj = 0;
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var e in objs)
                if (!e.isDefaultOrNull())
                    yield return e;
        }
    }

    public class UnnulSTDLists<T> : UnnullableLists<T> where T : iSTD
#if PEGI
        , iPEGI
#endif
        , new()
    {

        public override bool Decode(string tag, string data)
        {
            List<T> el; 
            int index = tag.ToIntFromText();
            this[index] = data.DecodeInto(out el);
            return true;
        }

        public override stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            List<int> inds;
            List<List<T>> vals = GetAllObjs(out inds);

            for (int i = 0; i < inds.Count; i++)
                cody.Add_ifNotEmpty(inds[i].ToString(), vals[i]);

            return cody;
        }

     //   public const string storyTag = "TreeObj";
      //  public override string getDefaultTagName() { return storyTag; }
    }
    
    public class VariableBranch
    {
        public int value;
        public VariableBranch[] br;
    }

    public static class ExtensionsForGenericCountless
    {
        #if PEGI
        public static bool edit_PEGI<G, T>(this G Cstd, ref int edited) where G : CountlessSTD<T>, IEnumerable where T: iSTD, iPEGI
            
            , new() {

            bool changed = false;
            
            if (edited > -1) {
                var e = Cstd[edited];
                if (e.isDefaultOrNull() || icon.Back.Click())
                    edited = -1;
                else
                    changed |= e.Try_Nested_Inspect();
                /*{
                    var pg = e as iPEGI;
                    if (pg != null)
                        changed |= pg.PEGI();
                }*/
            }

            if (edited == -1)
                foreach (var e in Cstd)
                    changed |= e.Name_ClickInspect_PEGI<T>(null, Cstd.currentEnumerationIndex, ref edited, null).nl();
            
            
            pegi.newLine();
            return changed;
        }
#endif
        public static stdEncoder Encode(this Countless<string> c)
        {
            var cody = new stdEncoder();
            List<int> inds;
            List<string> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add_String(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto(this string data, out Countless<string> c)
        {
            c = new Countless<string>();
            var cody = new stdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToIntFromText()] = cody.getData();

        }

        public static stdEncoder Encode(this Countless<float> c)
        {
            var cody = new stdEncoder();
            List<int> inds;
            List<float> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto(this string data, out Countless<float> c)
        {
            c = new Countless<float>();
            var cody = new stdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToIntFromText()] = cody.getData().ToFloat();

        }

        public static stdEncoder Encode(this Countless<Vector3> c)
        {
            var cody = new stdEncoder();
            if (c != null)
            {
                List<int> inds;
                List<Vector3> vals = c.GetAllObjs(out inds);
                for (int i = 0; i < inds.Count; i++)
                    cody.Add(inds[i].ToString(), vals[i]);
            }
            return cody;
        }

        public static void DecodeInto(this string data, out Countless<Vector3> c)
        {
            c = new Countless<Vector3>();
            var cody = new stdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToIntFromText()] = cody.getData().ToVector3();

        }

        public static stdEncoder Encode(this Countless<Quaternion> c)
        {
            var cody = new stdEncoder();
            List<int> inds;
            List<Quaternion> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto(this string data, out Countless<Quaternion> c)
        {
            c = new Countless<Quaternion>();
            var cody = new stdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToIntFromText()] = cody.getData().ToQuaternion();

        }

       /* public static stdEncoder EncodeText(this Countless<String> c)
        {
            var cody = new stdEncoder();
            List<int> inds;
            List<String> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.AddText(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeIntoText(this string data, out Countless<String> c)
        {
            c = new Countless<String>();
            var cody = new stdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToIntFromText()] = cody.getData();
        }*/

        public static int Get(this UnnullableSTD<CountlessInt> unn, int group, int index)
        {
            var tg = unn.GetIfExists(group);
            if (tg == null)
                return 0;
            return tg[index];
        }

        public static bool Get(this UnnullableSTD<CountlessBool> unn, int group, int index) {
            var tg = unn.GetIfExists(group);
            if (tg == null)
                return false;
            return tg[index];
        }

        public static T Get<T>(this UnnullableSTD<CountlessSTD<T>> unn, int group, int index) where T: iSTD, new()
        {
            var tg = unn.GetIfExists(group);
            if (tg == null)
                return default(T);
            return tg[index];
        }

        public static T GetIfExists<T>(this UnnullableSTD<T> unn, int index) where T : iSTD, new()
        {
            if (unn != null)
                return unn.GetIfElementExists(index);
            return default(T);
        }

    }


}