// Terms need to be a subclass of formula that just has the operation and RHS voided

namespace Formuthorpe;

public class Formula<T> : FormObject
{
    /* Fields 
     * parentForms: A list of parent formulas
     * lhs:         Left hand side of the operation, only operand if unary
     * rhs:         Right hand side of the operation, null if unary
     * operation:   function delegate based on operator
     */

    private List<Formula<T>> parentForms = new List<Formula<T>>();
    private FormObject lhs;
    private FormObject rhs;
    private Func<T> operation;
    private T val;

    public Formula (FormObject left, FormObject right, Func<T> op)
    {
        lhs = left;
        rhs = right;
        operation = op; 
    }

    public void update()
    {
        this.val = this.evaluate();
        foreach (Formula<T> form in this.parentForms)
        {
            form.update();
        }
    }
    
    public T evaluate()
    {
        return this.operation?.Invoke();
    }
    
    public void addParent(FormObject newParent)
    {
        this.parentForms.Add(newParent);
    }

    public static Formula<T> operator +(Formula<T> a, FormObject b)
    {
        Formula<T> retForm = new Formula<T>(a, b, () => a.addition()); 
        a.addParent(retForm);
        b.addParent(retForm);
        return retForm;
    }

    private T addition()
    {
        return (this.lhs.evaluate + this.rhs.evaluate);
    }
}

public class Term<T> : Formula
{
    /* Fields
     *  parentForms: A list of tuples of parent formulas and this term's index in their operands
     *  val:         value held by the term 
     */
    private List<Formula<T>> parentForms = new List<Formula<T>>();
    private T val; 
    private Formula lhs;
    private Formula rhs = null;
    private Func<T> operation = null;


    public Term(T initVal)
    {
        val = initVal; 
        lhs = new Formula(initVal, null, null);
    }

    /*
    * Now covered by superclass
    public void addParent(FormObject newParent)
    {
        this.parentForms.Add(newParent);
    }
    */

    public T evaluate()
    {
        return this.val;
    }

    // replace this with = operator overload I guess?
    public void setValue (T newVal)
    {
        this.val = newVal;
        this.lhs = new Formula(newVal, null, null);
        foreach (Formula<T> form in this.parentForms)
        {
            form.update();
        }
    }
}

