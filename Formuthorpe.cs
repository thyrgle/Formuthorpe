namespace Formuthorpe;

public class Formula<T>
{
    /* Fields 
     * parentForms: A list of parent formulas
     * lhs:         Left hand side of the operation, only operand if unary
     * rhs:         Right hand side of the operation, null if unary
     * operation:   function delegate based on operator
     */

    private List<Formula> parentForms = new List<Formula>();
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
    
    public void addParent(FormObject newParent)
    {
        this.parentForms.append(newParent);
    }

    public static Formula operator +(FormObject a, FormObject b)) 
    {
        retForm = new Formula(a, b, addition); 
        a.addParent(retForm);
        b.addParent(retForm);
        return retForm;
    }

    private T addition()
    {
        return (this.lhs.evaluate + this.rhs.evaluate);
    }
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

public union FormObject(Formula, Term)
