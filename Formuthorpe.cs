namespace Formuthorpe;

/* To figure out:
 * Constructors and making / modifying the parental tree for formObjects
 */

public class Formula<T>
{
    /* Fields 
     * parentForms: A list of tuples of parent formulas and the index of this formula in their operands
     * operands:    1 or 2 form objects that make up the formula, in order 
     * operation:   function delegate based on supplied operator
     */

    private List<(Formula form, int index)> parentForms;
    private List<FormObject> operands;
    private Func<List<FormObject>>  operation;

    public void update(FormObject child, int index)
    {
        this.operands(index) = child;
        foreach (Formula form in this.parentForms)
        {
            form.update(this, this.parentForms.index);
        }
    }
    
    public T evaluate
    {
        return (this.operation(this.operands));
    }
    
    public void update() 
    {
        this.operation?.Invoke(this.operands);
    }

    // Add checking for correct number of operands etc
    private T operator +(List<FormObject> operands) 
    {
        return (operands.get(0).evaluate + operands.get(1).evaluate);
    }
    
    private T operator -(List<FormObject> operands) 
    {
        return (operands.get(0).evaluate - operands.get(1).evaluate);
    }

    private T operator /(List<FormObject> operands) 
    {
        return (operands.get(0).evaluate / operands.get(1).evaluate);
    }

    private T operator *(List<FormObject> operands) 
    {
        return (operands.get(0).evaluate * operands.get(1).evaluate);
    }

}

public class Term<T>
{
    /* Fields
     *  parentForms: A list of tuples of parent formulas and this term's index in their operands
     *  val:         value held by the term 
     */
    private List<(Formula form, int index)> parentForms;
    private T val; 

    public T evaluate
    {
        return this.val;
    }

    public setValue (T newVal)
    {
        this.val = newVal;
        foreach (Formula form in this.parentForms)
        {
            form.update(this, form.index);
        }
    }
}

public union FormObject(Formula, Term)
