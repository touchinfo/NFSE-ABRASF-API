namespace NFSE_ABRASF.Exceptions
{
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("Ocorreram um ou mais erros de validação.")
        {
            Errors = errors;
        }
    }
}