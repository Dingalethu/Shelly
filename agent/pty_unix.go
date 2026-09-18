//go:build !windows

package main

import (
	"os"
	"os/exec"

	"github.com/creack/pty"
)

type ptyHandle struct {
	ptmx *os.File
	cmd  *exec.Cmd
}

func startShell(shell string) (*ptyHandle, error) {
	if shell == "" {
		shell = os.Getenv("SHELL")
		if shell == "" {
			shell = "/bin/sh"
		}
	}
	cmd := exec.Command(shell)
	cmd.Env = append(os.Environ(), "TERM=xterm-256color")

	ptmx, err := pty.Start(cmd)
	if err != nil {
		return nil, err
	}
	_ = pty.Setsize(ptmx, &pty.Winsize{Rows: 24, Cols: 80})
	return &ptyHandle{ptmx: ptmx, cmd: cmd}, nil
}

func (p *ptyHandle) Read(b []byte) (int, error)  { return p.ptmx.Read(b) }
func (p *ptyHandle) Write(b []byte) (int, error) { return p.ptmx.Write(b) }

func (p *ptyHandle) Resize(cols, rows uint16) error {
	return pty.Setsize(p.ptmx, &pty.Winsize{Rows: rows, Cols: cols})
}

func (p *ptyHandle) Close() error {
	_ = p.ptmx.Close()
	if p.cmd.Process != nil {
		_ = p.cmd.Process.Kill()
	}
	return nil
}