//go:build windows

package main

import (
	"github.com/UserExistsError/conpty"
)

type ptyHandle struct {
	cpty *conpty.ConPty
}

func startShell(shell string) (*ptyHandle, error) {
	if shell == "" {
		shell = "powershell.exe"
	}
	cpty, err := conpty.Start(shell, conpty.ConPtyDimensions(80, 24))
	if err != nil {
		return nil, err
	}
	return &ptyHandle{cpty: cpty}, nil
}

func (p *ptyHandle) Read(b []byte) (int, error)  { return p.cpty.Read(b) }
func (p *ptyHandle) Write(b []byte) (int, error) { return p.cpty.Write(b) }

func (p *ptyHandle) Resize(cols, rows uint16) error {
	return p.cpty.Resize(int(cols), int(rows))
}

func (p *ptyHandle) Close() error {
	return p.cpty.Close()
}