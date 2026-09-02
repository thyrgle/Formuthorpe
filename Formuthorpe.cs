namespace Formuthorpe;

public class Compound<T>
{
    /* Fields 
     * parentForms: A list of parent compound formula.
     * lhs:         Left hand side of the operation, only operand if unary
     * rhs:         Right hand side of the operation, null if unary
     * operation:   function delegate based on operator
     */

    private List<Compound> parentForms = new List<Formula>();
    private List<Formula> operands;
    private Func<T> operation;
    private T val;

    public Formula (List<Formula> operands, Func<T> op)
    {
        this.operands = operands;
        operation = op; 
    }

    public void update()
    {
        this.val = this.evalute;
        foreach (Formula form in this.parentForms)
        {
            form.update();
        }
    }
    
    public T evaluate
    {
        return this.operation?.Invoke();
    }
    
    private void addParent(FormObject newParent)
    {
        this.parentForms.append(newParent);
    }


    private static registerUnaryFunc(Func<T, T> op) {
	return Func<T, T> res = (a) => { 
	    Formula retForm = new Formula(a, op);
            a.addParent(retForm);
	    return retForm;
	}
    }

    private static registerBinaryFunc(Func<T, T> op) {
	return Func<T, T> res = (a, b) => { 
	    Formula retForm = new Formula(a, op);
            a.addParent(retForm);
	    b.addParent(retForm);
	    return retForm;
	}
    }

    public static Formula operator+ = registerBinaryFunc(Func<int, int, int> add = a + b);
    public static Formula operator- = registerBinaryFunc(Func<int, int, int> add = a - b);
    public static Formula operator* = registerBinaryFunc(Func<int, int, int> add = a * b);
    public static Formula operator/ = registerBinaryFunc(Func<int, int, int> add = a / b);
}

public class Term<T>
{
    /* Fields
     *  parentForms: A list of tuples of parent formulas and this term's index in their operands
     *  val:         value held by the term 
     */
    private List<Formula> parentForms = new List<Formula>();
    private T val; 

    public Term(T initVal)
    {
        val = initVal; 
    }

    public T evaluate
    {
        return this.val;
    }

    // replace this with = operator overload I guess?
    public void setValue (T newVal)
    {
        this.val = newVal;
        foreach (Formula form in this.parentForms)
        {
            form.update();
        }
    }
}

public union Formula(Compound, Term)
