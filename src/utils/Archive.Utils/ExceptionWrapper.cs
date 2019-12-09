using System;
using System.IO;

namespace Archive.Utils
{
    public static class ExceptionWrapper
    {
        public static TRes Wrap<TExc, TRes>(Func<TRes> func, string message = null) where TExc : Exception where TRes : class
        {
            try
            {
                var res = func();
                return res;
            }
            catch (ArgumentException e)
            {
                throw GetException<TExc>(message, e);
            }
            catch (DirectoryNotFoundException e)
            {
                throw GetException<TExc>("Directory not found", e);
            }
            catch (FileNotFoundException e)
            {
                throw GetException<TExc>("File not found", e);
            }
            catch (IOException e)
            {
                throw GetException<TExc>("I/O error", e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetException<TExc>("Don't have access rights", e);
            }
            catch (NotSupportedException e)
            {
                throw GetException<TExc>(message, e);
            }
            catch (Exception e)
            {
                throw GetException<TExc>(message, e);
            }
        }

        public static void Wrap<TExc>(Action action, string message = null) where TExc : Exception
        {
            try
            {
                action();
            }
            catch (ArgumentException e)
            {
                throw GetException<TExc>(message, e);
            }
            catch (DirectoryNotFoundException e)
            {
                throw GetException<TExc>("Directory not found", e);
            }
            catch (FileNotFoundException e)
            {
                throw GetException<TExc>("File not found", e);
            }
            catch (IOException e)
            {
                throw GetException<TExc>("I/O error", e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetException<TExc>("Don't have access rights", e);
            }
            catch (NotSupportedException e)
            {
                throw GetException<TExc>(message, e);
            }
            catch (Exception e)
            {
                throw GetException<TExc>(message, e);
            }
        }

        public static TExc GetException<TExc>(string message = null, Exception innerException = null) where TExc : Exception
        {
            if (message == null)
            {
                return Activator.CreateInstance<TExc>();
            }

            var constructor = typeof(TExc).GetConstructor(new[] { typeof(string) });
            var exc = (TExc)constructor.Invoke(new object[] { message, innerException });
            return exc;
        }
    }
}