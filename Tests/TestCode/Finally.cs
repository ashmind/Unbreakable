using System;

class C {
    static void M() {
        try {
            throw new Exception();
        }
        finally {
            MF();
        }
    }

    static void MF()
    {
    }
}